# Asgard Framework Architecture

## 整体架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Asgard Host                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │
│  │  Yggdrasil  │  │  Config    │  │  Modules   │                  │
│  │   Host      │  │  Loading   │  │  Discovery │                  │
│  └─────────────┘  └─────────────┘  └─────────────┘                  │
└─────────────────────────────────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────────────────────────────────┐
│                      Plugins (Optional)                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │
│  │  Built-in   │  │  External  │  │  Dynamic    │                  │
│  │  Plugins    │  │  Plugins   │  │  Loading   │                  │
│  └─────────────┘  └─────────────┘  └─────────────┘                  │
└─────────────────────────────────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────────────────────────────────┐
│                     Services & Features                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │  Database  │  │   Cache    │  │  Messaging  │  │  Jobs      │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │  Security  │  │ Encryption │  │  Identity  │  │  Context   │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────────────────────────────────┐
│                     Controllers / APIs                             │
│              BaseController → Response<T> → HTTP Response          │
└─────────────────────────────────────────────────────────────────────┘
```

## 核心设计原则

| 原则 | 说明 |
|------|------|
| **模块化** | 每个能力都是可选模块，通过配置启用/禁用 |
| **插件化** | 业务功能作为插件加载，宿主只负责编排 |
| **可扩展** | 框架核心不硬编码业务，通过插件扩展功能 |
| **约定优先** | 基于约定自动扫描注册，减少配置 |
| **可选降级** | 未启用的模块返回 null，优雅降级 |
| **一步构建** | 插件编译后直接放入目录，无需重新编译宿主 |

## 核心入口

| 入口 | 使用场景 |
|------|----------|
| `YggdrasilHost.CreateBuilder()` | 创建宿主构建器 |
| `PluginWebAppDefaults.RunAsync<TPlugin>()` | 单插件运行入口 |
| `builder.UseBuiltInPlugin<TPlugin>()` | 注册内建插件 |
| `AbsAsgardContext` | 公共能力统一入口 |

## 模块索引

| 模块 | 对应 Skill | 说明 |
|------|------------|------|
| asgard-host-project | `$asgard-host-project` | 宿主项目构建与启动 |
| asgard-configuration | `$asgard-configuration` | 配置体系、YAML、ConfigPath |
| asgard-host-features | `$asgard-host-features` | 静态文件、认证、Swagger、限流、健康检查 |
| asgard-api-development | `$asgard-api-development` | Web API、控制器、统一响应 |
| asgard-plugin-development | `$asgard-plugin-development` | 插件实现与约定 |
| asgard-plugin-lifecycle | `$asgard-plugin-lifecycle` | 插件生命周期、阶段、状态机 |
| asgard-context-usage | `$asgard-context-usage` | AbsAsgardContext 使用 |
| asgard-base-types | `$asgard-base-types` | 基类、响应模型、继承语义 |
| asgard-repository-service-registration | `$asgard-repository-service-registration` | 仓储扫描、服务注册、约定装配 |
| asgard-database | `$asgard-database` | 数据库、仓储模式、EF Core |
| asgard-cache | `$asgard-cache` | 多级缓存、内存 + Redis |
| asgard-messaging | `$asgard-messaging` | 消息队列、发布订阅 |
| asgard-job-scheduling | `$asgard-job-scheduling` | 作业调度、定时任务 |
| asgard-security | `$asgard-security` | 认证、授权、安全 |
| asgard-dotnet-10-csharp-14 | `$asgard-dotnet-10-csharp-14` | 编码规范、最佳实践 |

## 调用链示例

```
Program.cs
  ↓
YggdrasilHost.CreateBuilder()
  ↓
builder.UseBuiltInPlugin<MyPlugin>()
  ↓
Plugin → ConfigureServices() → 注册服务、加载配置
  ↓
Plugin → InitializeAsync() → 异步初始化
  ↓
Build() → RunAsync() → 启动宿主
  ↓
HTTP Request → Controller → Service → Repository → Database
  ↓
AbsAsgardContext.Cache → 缓存读取/写入
  ↓
Response → JSON → Client
```

## 生命周期阶段

1. **配置阶段** - 加载配置文件、环境变量、命令行
2. **服务注册阶段** - 插件注册各自服务
3. **初始化阶段** - 插件异步初始化（连接数据库、验证配置）
4. **启动阶段** - 启动 HTTP 服务器
5. **运行阶段** - 处理请求
6. **停止阶段** - 优雅停止

## 推荐目录结构

```
src/
├── Common/                    # 公共基础设施
│   ├── Asgard.Abstractions/   # 抽象定义
│   └── Asgard.Core/           # 核心实现
├── Host/                      # 宿主项目
│   └── Asgard.Host/
├── Plugins/                   # 插件项目
│   ├── Asgard.Users/
│   ├── Asgard.Orders/
│   └── ...
└── config/                    # 配置文件
    └── app.yaml
```

## 参考资料

- `doc/01-框架概览.md` - 框架设计概述
- `doc/02-快速开始.md` - 快速开始指南
- `doc/09-源码参考索引.md` - 源码文件索引
