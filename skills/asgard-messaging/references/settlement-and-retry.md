# RabbitMQ 确认、重试与恢复

适用于包含 Asgard 6.0.1 基础设施生命周期修复（2026-10-08 核对；发布状态另行确认）的目标源码。先核对目标版本及 `SubscribeOptions`，不要把修复当作所有旧包已具有的行为。源码入口为 Asgard 的 `src/doc/18-消息队列.md`、`RabbitMQDeliverySettlement.cs`、`RabbitMQMessageQueue.Delivery.cs`、`RabbitMQMessageQueue.cs` 及相关消息工厂/拓扑文件。

## 保持现有调用方式

- `AutoAck=false`、`MaxRetryCount=3`、`EnableDeadLetter=true` 默认值不变；全局 `MQConfig.EnableDeadLetterQueue` 默认也为 true
- 默认手动确认：业务成功后调用 `context.AcknowledgeAsync()`。handler 正常返回不会由框架补 ACK，不必为本补丁重写已有成功处理器
- `AutoAck=true` 是 broker no-ack 模式，投递即确认；上下文 ACK/Reject 都是无操作，处理失败不自动重试。不能用它换取可靠处理
- 同一次投递的确认、拒绝与框架失败路径共享一次性状态；已 ACK 再抛异常不会重复 NACK。一次结算尝试失败/结果不确定时也不自动改发另一个 ACK/NACK
- 业务确认之前完成幂等写入；不要吞掉失败后 ACK，也不要提前 ACK 再继续关键业务

## 有限自动重试

手动确认订阅的 handler/反序列化异常会触发有限自动重试：原始消息体、现有 MessageId 和 AMQP 属性保留，`X-Asgard-Retry-Count` 写入并跨投递保留。初次为 0，默认最多再尝试 3 次。

重试通过默认交换机精确发回源队列，不向原 topic 的其他订阅者重播。首次转发还保存原 routing key/exchange，使后续工厂仍恢复原业务键回退值与上下文 Topic。不要用消息重新序列化或每次生成新 MessageId 代替该流程。

- 转发为 `mandatory=true`，开启 publisher confirms 和确认跟踪；仅确认成功且可路由后才 ACK 原投递
- 不可路由返回、broker NACK、断线或 30 秒确认等待超时保留原投递未确认，不立即 requeue 形成热循环
- 修复目标队列/连接后重建通道或连接；原通道关闭后未确认投递才可恢复。仅 Unsubscribe/BasicCancel/重新订阅不释放旧通道上的未确认投递
- 转发与原消息 ACK 不是原子事务，确认丢失/恢复可能重复处理；不能声称 exactly-once，必须业务幂等
- `context.RejectAsync(true)` 仍直接 broker requeue，不增加此自动重试计数，不能把它当有限重试接口
- `RetryIntervals` 和独立重试策略类不会自动给上述转发加退避；不要承诺配置它们就能延迟重试
- `PullAsync` 解析失败也执行默认有限重试，但继续向调用者抛解析异常；成功拉取后的业务异常由调用者决定确认/拒绝，不等同订阅 handler 自动失败路径

## 死信与拓扑

重试耗尽或订阅上下文 `RejectAsync(false)` 使用同样的 confirmed + mandatory 转发到死信目的队列，再确认原投递。默认目的地为完整源队列名加 `.dlq`；自定义 `DeadLetterQueue` 使用配置的 QueuePrefix。

- 全局或订阅关闭死信时，耗尽后明确 ACK 丢弃，不靠 NACK 触发源队列旧 DLX；这是丢弃策略，设置前按业务后果选择
- `AutoDeclare=true` 声明实际死信队列，但不把它绑定到正常 topic/fanout 流量，避免健康消息误入死信
- 已有源队列 DLX 参数保持兼容重声明；框架失败转发不依赖旧 DLX。broker TTL/队列长度触发的死信仍需部署侧单独配置并验证 DLX 拓扑
- `AutoDeclare=false` 不创建交换机、队列或绑定；部署方预建源队列，以及手动确认且启用死信时的实际目标队列；`AutoAck=true` 只检查源队列，不要求不会使用的死信队列。框架只被动检查存在性，缺失在消费开始前失败
- mandatory 还检测运行中目的队列消失。不要为了恢复擅自删除生产队列或更改消费者拓扑

## 验证边界

修改默认值、确认顺序、转发或拓扑时，测试手动/自动确认、已确认后抛异常、坏 JSON、重试耗尽、不可路由、确认失败/超时、精确源队列和属性保留。该补丁除通道替身回归外，已于 2026-10-08 在隔离的本地 RabbitMQ 4.0.5 单节点通过 8 项真实 broker 测试：默认手动确认、no-ack、确认后抛异常、默认 3 次重试与原始体/属性保留、不向其他订阅者重播、坏 JSON 死信、目的队列缺失时保留原投递及关闭通道后的恢复、外部拓扑检查。对应源码为 `RabbitMQBrokerIntegrationTests.cs` 与 `.Recovery.cs`。这不代表生产集群、故障转移、重启持久性、网络分区/断线时序、TLS、broker TTL/队列长度 DLX 或长时间压力验收；上线仍需对应部署验证。
