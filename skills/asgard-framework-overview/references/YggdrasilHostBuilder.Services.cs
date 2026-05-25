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
            _ = services.AddSingleton(_databaseConfig!);
            _ = services.AddSingleton(_mqConfig!);
            _ = services.AddSingleton(_jobConfig!);

            _ = services.AddAsgardSerilog(_logConfig!);
            _ = services.AddAsgardObservabilityQueries();

            RegisterInfrastructureManagers(services);

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
            RegisterTsGenerationServices(services);
            RegisterHostedServices(services);

            if (_hostConfig.Cors is { Enabled: true })
            {
                _ = services.ConfigureOptions<PluginCorsPostConfigureOptions>();
            }

            _afterServiceRegistration?.Invoke(services);

            if (_hostConfig.Swagger is { Enabled: true })
            {
                _ = services.AddSwaggerGen(options =>
                {
                    // 文档元数据来自主机配置，XML 注释路径则按实际参与运行的程序集动态收集。
                    options.SwaggerDoc(_hostConfig.Swagger.Version, new Microsoft.OpenApi.OpenApiInfo
                    {
                        Title = _hostConfig.Swagger.Title,
                        Version = _hostConfig.Swagger.Version,
                        Description = _hostConfig.Swagger.Description
                    });

                    foreach (var xmlPath in GetSwaggerXmlCommentPaths())
                    {
                        options.IncludeXmlComments(xmlPath);
                    }

                    ConfigureSwaggerSecurity(options);
                });

                _logger.LogDebug(
                    "Swagger services enabled: {Title} {Version}",
                    _hostConfig.Swagger.Title,
                    _hostConfig.Swagger.Version);
            }

            _logger.LogInformation("[Phase 4/5] Service registration completed");
        });
    }

    /// <summary>
    /// 将启动阶段预构建好的基础设施管理器回填到最终应用容器。
    /// </summary>
    /// <param name="services">应用服务集合。</param>
    private void RegisterInfrastructureManagers(IServiceCollection services)
    {
        // 即使缓存模块被显式关闭，也要提供一个可注入的空缓存实现。
        // 这样业务仓储可以继续统一依赖 IMultiLevelCache，而不需要为“禁用缓存”分支改写构造函数。
        if (_cacheConfig is not null && _cacheManager is null)
        {
            _ = services.AddSingleton<IMultiLevelCache>(new MultiLevelCache(null, null, _cacheConfig));
        }

        if (_cacheManager != null)
        {
            _ = services.AddSingleton<ICacheManager>(_cacheManager);
            _ = services.AddSingleton(_cacheManager.Cache);

            if (_cacheManager.MemoryCache != null)
            {
                _ = services.AddSingleton(_cacheManager.MemoryCache);
            }

            if (_cacheManager.DistributedCache != null)
            {
                _ = services.AddSingleton(_cacheManager.DistributedCache);
            }

            if (_cacheManager.ConnectionMultiplexer != null)
            {
                _ = services.AddSingleton(_cacheManager.ConnectionMultiplexer);
                // 数据保护密钥落到 Redis 后，多实例部署时才能共享票据解密能力。
                _ = services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(_cacheManager.ConnectionMultiplexer, "Asgard:DataProtection-Keys")
                    .SetApplicationName("Asgard");
            }
        }

        if (_mqManager != null)
        {
            _ = services.AddSingleton<IMQManager>(_mqManager);
            _ = services.AddSingleton(sp => sp.GetRequiredService<IMQManager>().MessageQueue);
        }

        if (_jobManager != null)
        {
            _ = services.AddSingleton<IJobManager>(_jobManager);
            _ = services.AddSingleton(sp => sp.GetRequiredService<IJobManager>().Scheduler);
        }

        if (_traceStore != null)
        {
            _ = services.AddSingleton(_traceStore);
        }
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
        if (_hostConfig.HealthCheck is not { Enabled: true })
        {
            return;
        }

        _ = services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: ["live", "ready"]);

        _logger.LogDebug("Health check services registered");
    }

    private void RegisterRateLimiting(IServiceCollection services)
    {
        if (_hostConfig.RateLimiting is not { Enabled: true } rateLimitingOptions)
        {
            return;
        }

        _ = services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return ValueTask.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                rateLimitingOptions.Policy switch
                {
                    "FixedWindow" => RateLimitPartition.GetFixedWindowLimiter(
                        "global",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitingOptions.PermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                            QueueLimit = rateLimitingOptions.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }),
                    "SlidingWindow" => RateLimitPartition.GetSlidingWindowLimiter(
                        "global",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitingOptions.PermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                            SegmentsPerWindow = rateLimitingOptions.SegmentsPerWindow,
                            QueueLimit = rateLimitingOptions.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }),
                    "TokenBucket" => RateLimitPartition.GetTokenBucketLimiter(
                        "global",
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = rateLimitingOptions.TokenLimit,
                            TokensPerPeriod = rateLimitingOptions.TokensPerSecond,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                            AutoReplenishment = true,
                            QueueLimit = rateLimitingOptions.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }),
                    _ => throw new InvalidOperationException($"Unsupported rate limiting policy: {rateLimitingOptions.Policy}")
                });
        });

        _logger.LogDebug("Rate limiting services registered");
    }

}
