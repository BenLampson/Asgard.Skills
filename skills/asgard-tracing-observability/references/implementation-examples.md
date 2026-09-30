## 代码示例

### 在业务服务里追加备注

```csharp
/// <summary>
/// 创建订单
/// </summary>
/// <param name="command">订单命令</param>
/// <returns>订单标识</returns>
public async Task<Guid> CreateOrderAsync(CreateOrderCommand command)
{
    AsgardContext.Trace?.AddTag("OrderId", command.OrderId.ToString());
    AsgardContext.Trace?.AddBranch("OrderCreate", "ValidateBeforePersist");
    AsgardContext.Trace?.AddNote("创建订单前已完成业务规则校验，可据此反推测试前置条件。");

    return await _orderRepository.InsertAsync(command.ToEntity());
}
```

### 在控制器里补充测试线索

```csharp
/// <summary>
/// 根据订单标识获取详情
/// </summary>
/// <param name="id">订单标识</param>
/// <returns>订单详情</returns>
[HttpGet("{id:guid}")]
public async Task<ActionResult<Response<OrderDto>>> GetByIdAsync(Guid id)
{
    AsgardContext.Trace?.AddTag("Endpoint", "Orders/GetById");
    AsgardContext.Trace?.AddNote("该接口默认期望有效 Guid 且命中已登录用户上下文。");

    var result = await _orderService.GetByIdAsync(id);
    return Success(result);
}
```

## 参数记录规则

- 简单类型：`string`、数值、`bool`、`Guid`、`DateTime`、`DateTimeOffset`、`TimeSpan`、枚举等，直接记录
- 字符串过长时，只保留前缀并附带长度
- 明显敏感字段（如 password、token、secret、authorization）默认掩码
- 集合类型只记录类型名和数量
- 复杂对象只记录类型名，以及 `Id`、`TenantId`、`UserId`、`Code`、`Name`、`Status`、`Type` 这类关键字段摘要
