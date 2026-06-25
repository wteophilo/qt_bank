using System.Threading;
using System.Threading.Tasks;

namespace QtBank.Api.Infrastructure.Messaging;

public interface IPubSubPublisher
{
    Task PublishAsync<T>(string topicName, T message, CancellationToken cancellationToken = default) where T : class;
}
