namespace Asgard.Abstractions.Messaging;

/// <summary>由宿主或独立调用方持有并释放的消息队列客户端。</summary>
public interface IMessageQueue : IAsgardMessageQueue, IAsyncDisposable
{
}
