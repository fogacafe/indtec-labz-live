using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Indtec.Labz.Live.Lambda.Core;

public abstract class BaseLambda<TRequest, TCommand, TResponse>
{
    protected abstract IServiceProvider Services { get; }

    public async Task<TResponse> FunctionHandler(TRequest request, ILambdaContext context)
    {
        await using var scope = Services.CreateAsyncScope();
        var command = Map(request, context);

        try
        {
            return await ExecuteAsync(scope.ServiceProvider, command, context);
        }
        catch (Exception exception)
        {
            OnUnhandledException(exception, context);
            throw;
        }
    }

    protected abstract TCommand Map(TRequest request, ILambdaContext context);

    protected abstract Task<TResponse> ExecuteAsync(
        IServiceProvider services,
        TCommand command,
        ILambdaContext context);

    protected virtual void OnUnhandledException(Exception exception, ILambdaContext context)
        => context.Logger.LogError($"Unhandled lambda exception. RequestId={context.AwsRequestId}. Error={exception}");
}