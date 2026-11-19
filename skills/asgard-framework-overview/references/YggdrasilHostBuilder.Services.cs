namespace Asgard.Yggdrasil.AspNetCore;

public partial class YggdrasilHostBuilder
{
    /// <summary>
    /// 执行服务注册阶段，将基础设施、框架能力和插件能力统一装配到宿主容器中。
    /// </summary>
    /// <param name="hostBuilder">当前宿主构建器。</param>
    private void ExecutePhase2ServiceRegistration(IHostBuilder hostBuilder)
    {
        _ = hostBuilder.ConfigureServices((context, services) =>
        {
            _beforeServiceRegistration?.Invoke(services);

            // 先注册已完成加载的配置对象，保证后续扩展方法都能从容器读取同一份配置。
            _ = services.AddSingleton(_hostConfig);
            _ = services.AddSingleton(_logConfig!);
            _ = services.AddSingleton(_traceConfig!);
            _ = services.AddSingleton(_cacheConfig!);
            services.TryAddSingleton(_distributedLockOptions!);
            _ = services.AddSingleton(_databaseConfig!);
            _ = services.AddSingleton(_mqConfig!);
            _ = services.AddSingleton(_jobConfig!);
            services.TryAddSingleton<ISystemConfig>(new EffectiveHostConfiguration(
                _hostConfig, _cacheConfig!, _mqConfig!, _jobConfig!, _databaseConfig!, _traceConfig!, _logConfig!));

            _ = services.AddAsgardSerilog(_logConfig!);
            _ = services.AddAsgardObservabilityQueries();

            RegisterInfrastructureManagers(services);
            _ = services.AddAsgardRedisMessaging();
            if (!string.IsNullOrWhiteSpace(_cacheConfig!.OutboxScope))
            {
                if (!_databaseConfig!.Enabled) throw new InvalidOperationException("缓存失效发件箱需要启用业务数据库。");
                _ = services.AddAsgardCacheOutbox();
            }

            if (_cacheConfig!.Enabled
                && !services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedLock)))
            {
                // Redis 可用时分布式锁属于宿主基础能力，无需业务模块重复声明注册。
                _ = services.AddDistributedLock(_distributedLockOptions!);
            }

            if (_databaseConfig!.Enabled)
            {
                _ = services.AddDatabase(_databaseConfig);
            }

            ArgumentNullException.ThrowIfNull(_asgardConfiguration);
            _ = services.AddAsgardSecurityServices(
                _asgardConfiguration.LoadSection<AsgardEncryptionOptions>(AsgardEncryptionOptions.SectionName));

            if (_hostConfig.Cors is { Enabled: true })
            {
                _ = services.AddCors(options =>
                {
                    // 默认策略覆盖全局场景，命名策略用于插件或业务模块按需扩展。
                    var defaultPolicy = _hostConfig.Cors.DefaultPolicy;
                    options.AddDefaultPolicy(builder => ConfigureCorsPolicy(builder, defaultPolicy));

                    if (_hostConfig.Cors.Policies is null)
                    {
                        return;
                    }

                    foreach (var (name, policy) in _hostConfig.Cors.Policies)
                    {
                        options.AddPolicy(name, builder => ConfigureCorsPolicy(builder, policy));
                    }
                });

                _logger.LogDebug("CORS services registered");
            }

            RegisterAuthenticationServices(services);
            RegisterHealthChecks(services);
            RegisterRateLimiting(services);

            _ = services.AddAsgardAspNetCore();
            var mvcBuilder = services.AddControllers(options =>
            {
                _ = options.Filters.AddService<AsgardTraceActionFilter>();
            });

            _ = services.AddAsgardContext();

            ConfigurePluginServices(services, mvcBuilder);
            if (_hostConfig.TsGen is { Enabled: true })
            {
                RegisterTsGenerationServices(services);
            }
            RegisterHostedServices(services);

            if (_hostConfig.Cors is { Enabled: true })
            {
                _ = services.ConfigureOptions<PluginCorsPostConfigureOptions>();
            }

            _afterServiceRegistration?.Invoke(services);
            PluginServiceConfigurator.ValidateConfigurationServices(services);
            // MVC、API Explorer 和 TS 导出共享同一份控制器发现边界。
            mvcBuilder.PartManager.FeatureProviders.Add(new PluginControllerFeatureProvider(_pluginDescriptors));
            mvcBuilder.PartManager.PopulateFeature(new ControllerFeature());

            RegisterOpenApi(services);

            _logger.LogInformation("[Phase 4/5] Service registration completed");
        });
    }

    /// <summary>
    /// 注册 Asgard 运行时托管服务，使插件与运行时逻辑随宿主生命周期启动。
    /// </summary>
    /// <param name="services">应用服务集合。</param>
    private static void RegisterHostedServices(IServiceCollection services)
    {
        _ = services.AddHostedService<AsgardRuntimeHostedService>();
    }

    private void RegisterHealthChecks(IServiceCollection services)
    {
        if (_hostConfig.HealthCheck is not { Enabled: true } options)
        {
            return;
        }

        var checks = services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: ["live", "ready"]);

        foreach (var feature in options.RequiredFeatures.Distinct())
        {
            _ = checks.Add(new HealthCheckRegistration($"asgard.{feature}",
                provider => new RequiredFeatureHealthCheck(feature, provider), HealthStatus.Unhealthy, ["ready"]));
        }
        _ = services.PostConfigure<HealthCheckServiceOptions>(configured =>
        {
            foreach (var registration in configured.Registrations)
                if (registration.Timeout == Timeout.InfiniteTimeSpan)
                    registration.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        _logger.LogDebug("Health check services registered");
    }

    private void RegisterRateLimiting(IServiceCollection services)
    {
        if (_hostConfig.RateLimiting is not { Enabled: true } rateLimitingOptions)
        {
            return;
        }

        if (rateLimitingOptions.UseRedis)
        {
            if (_cacheConfig?.Enabled != true)
                throw new InvalidOperationException("host.rateLimiting.useRedis 需要启用 caching.enabled。");
            services.TryAddSingleton(sp => new RedisQuotaStore(sp.GetRequiredService<IConnectionMultiplexer>(), _cacheConfig.Redis));
        }

        RedisQuotaStore? GetQuotaStore(IServiceProvider services) =>
            rateLimitingOptions.UseRedis ? services.GetRequiredService<RedisQuotaStore>() : null;

        _ = services.AddRateLimiter(YggdrasilRateLimiterFactory.ConfigureRejection);
        _ = services.AddOptions<RateLimiterOptions>().Configure<IServiceProvider>((options, provider) =>
            options.GlobalLimiter = YggdrasilRateLimiterFactory.CreatePreAuthenticationLimiter(rateLimitingOptions, GetQuotaStore(provider)));

        if (rateLimitingOptions.User is { Enabled: true } userRateLimitingOptions)
            _ = services.AddSingleton(sp => new YggdrasilUserRateLimiter(userRateLimitingOptions, GetQuotaStore(sp)));

        _logger.LogDebug(
            "Rate limiting services registered. IP layer enabled={IpEnabled}. User layer enabled={UserEnabled}",
            rateLimitingOptions.Ip?.Enabled == true,
            rateLimitingOptions.User?.Enabled == true);
    }

}
