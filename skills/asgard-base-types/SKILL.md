---
name: asgard-base-types
description: Asgard 基类与基础模型 skill。Use when another AI needs the meaning, key fields, inheritance points, and correct usage timing of BaseController, Response models, PluginBase, AbsAsgardContext, HostConfig, PluginConfig, or other framework base abstractions.
---

# Asgard 核心基类与基础模型

## 作用

本 skill 说明 Asgard 框架提供的核心基类和基础模型的用法。

什么时候使用本 skill：
- 需要实现控制器时，确定如何继承 `BaseController`
- 需要实现插件时，确定如何继承 `PluginBase`
- 需要使用统一 API 响应格式时
- 需要获取框架公共能力时，确定如何使用 `AbsAsgardContext`
- 不清楚某个基类有哪些核心字段时

## 核心基类速查表

| 基类 | 作用 | 使用场景 |
|------|------|----------|
| `BaseController` | Web API 控制器基类 | 所有控制器继承它 |
| `PluginBase` | 插件基类 | 所有插件继承它 |
| `AbsAsgardContext` | 框架统一上下文 | 注入它获取缓存、消息、作业等能力 |
| `Response<T>` | 统一 API 响应模型 | 所有非分页 API 默认返回此类型 |
| `PageResponse<T>` | 页码分页响应 | 所有页码分页查询必须返回 |
| `CursorResponse<T>` | 游标分页响应 | 所有游标分页/无限滚动查询必须返回 |
| `HostConfig` | 宿主根配置 | 宿主全局配置 |
| `PluginConfig` | 插件系统根配置 | 插件系统配置 |

## BaseController（控制器基类）

**用法：**

```csharp
namespace {Namespace}.Controllers;

public class {ControllerName}Controller : BaseController
{
    public {ControllerName}Controller(AbsAsgardContext asgardContext)
        : base(asgardContext)
    {
    }

    // 你的 Action 方法...
}
```

**核心特性：**
- 自动注入 `AbsAsgardContext` 并暴露为 `protected` 字段
- 提供便捷响应方法：`Success(data)`, `SuccessPage(...)`, `SuccessCursor(...)`, `Fail(code, message)`, `NotFound(...)`, `BadRequest(...)`
- 所有响应自动包装为统一格式

**硬约束：**
- 所有控制器都必须继承 `BaseController`
- 统一响应模型只约束 Controller 对外返回，不约束 Service / Repository 的内部返回类型
- Service 层负责产出 DTO，Controller 层负责把 DTO 转成 VO 并包装成统一响应
- 所有 Action 都必须返回 `Response<T>`、`Response<object>`、`PageResponse<T>` 或 `CursorResponse<T>`
- 不允许 Controller 直接返回裸对象、裸集合、基元类型或匿名对象

## 统一响应模型

Asgard 提供四种响应模型：

| 类型 | 用途 |
|------|------|
| `Response<T>` | 标准单对象响应 |
| `Response<object>` | 无数据响应 |
| `PageResponse<T>` | 页码分页（带 totalCount）|
| `CursorResponse<T>` | 游标分页（瀑布流/无限滚动）|

**强制规则：**

- 单对象、详情、创建、修改、删除等接口由 Controller 统一返回 `Response<T>` 或 `Response<object>`
- 页码分页接口由 Controller 统一返回 `PageResponse<T>`
- 游标分页接口由 Controller 统一返回 `CursorResponse<T>`
- 不允许自己再定义另一套“通用 API 响应模型”替代 `Response` 家族

**示例：**

```csharp
// 成功返回数据
return Success(data);

// Controller 中先把 DTO 列表转成 VO，再返回分页响应
return SuccessPage(vos, totalCount, page, size);

// 失败
return Fail(400, "参数错误");

// 未找到
return NotFound<User>("用户不存在");
```

通过 `Response` 静态工厂创建：

```csharp
// 创建成功响应
var response = Response.Success(data);

// 创建失败响应
var response = Response.Fail<User>(404, "Not found");
```

## PluginBase（插件基类）

**用法：**

```csharp
namespace {Namespace};

public class {PluginName}Plugin : PluginBase
{
    public override string Id => "{plugin_id}";
    public override string Name => "{plugin_name}";
    public override Version Version => new({major}, {minor}, {patch});

    public override string Description => "{description}";

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        var logger = CreateLogger();
        logger.LogInformation("Plugin {PluginId} initialized", Id);
        return Task.CompletedTask;
    }
}
```

**必须实现：**
- `Id` - 插件唯一标识
- `Name` - 插件显示名称
- `Version` - 版本号

**可重写钩子：**
- `OnConfigureServicesAsync` - 注册服务
- `OnInitializeAsync` - 初始化（此时 `GetService` 可用）
- `OnStartAsync` - 启动
- `OnStopAsync` - 停止
- `OnDisposeAsync` - 释放

**便捷方法（在 `OnInitializeAsync` 及之后可用）：**
- `GetService<T>()` - 获取必需服务
- `GetOptionalService<T>()` - 获取可选服务
- `GetAsgardContext()` - 获取框架上下文
- `CreateLogger()` - 创建日志器

**⚠️ 重要约束：**
- `ServiceProvider` 只在 `InitializeAsync` 之后可用
- 在构造函数和 `ConfigureServicesAsync` 中不能调用 `GetService()`

## AbsAsgardContext（框架统一上下文）

**作用：** 聚合所有可选模块能力，通过属性暴露。所有属性都是 `T?` 类型，未启用对应模块时返回 `null`。

**用法：**

```csharp
namespace {Namespace}.Services;

public class {ServiceName}Service : I{ServiceName}Service
{
    private readonly AbsAsgardContext _context;

    public {ServiceName}Service(AbsAsgardContext context)
    {
        _context = context;
    }

    public async Task<{ResultType}> DoSomethingAsync()
    {
        // 使用缓存（检查 null，模块可能未启用）
        var cache = _context.Cache;
        if (cache != null)
        {
            var cached = await cache.GetAsync<{ResultType}>(key);
            if (cached != null) return cached;
        }

        // 业务逻辑...

        // 发布消息
        var mq = _context.MessageQueue;
        if (mq != null)
        {
            await mq.PublishAsync(topic, data);
        }

        return result;
    }
}
```

**主要属性：**

| 属性 | 能力 |
|------|------|
| `Cache` | 多级缓存 |
| `JobScheduler` | 作业调度 |
| `MessageQueue` | 消息队列 |
| `IdentityContext` | 当前身份信息 |
| `TenantScopeFactory` | 租户作用域工厂 |
| `Encryption` | 加密服务 |
| `PasswordHasher` | 密码哈希 |

**设计原则：**
- 生命周期：Scoped，每次请求一个新实例
- 可选模块：未启用返回 `null`，调用方检查并优雅降级
- 避免循环依赖：模块间通过 Context 间接引用，不直接注入

## 代码模板

完整模板见 `templates/` 目录：
- `BaseController.cs.template` - 控制器基类模板
- `PluginBaseImplementation.cs.template` - 插件实现模板
- `AbsAsgardContextInjection.cs.template` - 上下文注入模板
- `ResponseSuccess.cs.template` - API 响应示例模板

## 参考源码

需要查看完整定义时读 `references/`：
- `BaseController.cs` - 控制器基类
- `ResponseStaticFactory.cs` - 响应工厂方法
- `PluginBase.cs` - 插件基类
- `AbsAsgardContext.cs` - 框架上下文抽象

## 不要这样做

- ❌ 不要为了"更灵活"而绕开现有基类，自己复制同名字段
- ❌ 不要在 `ConfigureServicesAsync` 或构造函数中调用 `GetService()` / `CreateLogger()`
- ❌ 不要误把 `PluginBase.ServiceProvider` 当作任何阶段都可用
- ❌ 不要在已有 `Response` 家族的情况下，自己再定义另一套通用响应格式
- ❌ 不要把 Controller 对外统一响应的规则错误地下沉到 Service / Repository 层
- ❌ 不要让 Controller 直接返回裸 `VO`、`DTO`、集合或基元值
- ❌ 不要忘了检查 `AbsAsgardContext` 的属性是否为 `null`（因为模块可能未启用）
