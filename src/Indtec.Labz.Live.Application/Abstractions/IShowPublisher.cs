using Indtec.Labz.Live.Application.Shows;

namespace Indtec.Labz.Live.Application.Abstractions;

public interface IShowPublisher
{
    Task PublishScheduledAsync(ShowScheduled integrationEvent, CancellationToken cancellationToken);
}