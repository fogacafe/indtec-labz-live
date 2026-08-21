namespace Indtec.Labz.Live.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<bool> TryStartAsync(string key, CancellationToken cancellationToken);
    Task CompleteAsync(string key, CancellationToken cancellationToken);
    Task AbandonAsync(string key, CancellationToken cancellationToken);
}