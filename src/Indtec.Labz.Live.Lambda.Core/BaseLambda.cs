using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Indtec.Labz.Live.Lambda.Core;

public abstract class BaseLambda<TRequest, TCommand, TResponse>
{
    private static readonly Lazy<IServiceProvider> RootProvider = new(BuildRootProvider, LazyThreadSafetyMode.ExecutionAndPublication);

    public async Task<TResponse> FunctionHandler(TRequest request, ILambdaContext context)
    {
        await using var scope = RootProvider.Value.CreateAsyncScope();
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

    protected abstract void ConfigureServices(IServiceCollection services);

    protected virtual void OnUnhandledException(Exception exception, ILambdaContext context)
        => context.Logger.LogError($"Unhandled lambda exception. RequestId={context.AwsRequestId}. Error={exception}");

    private static IServiceProvider BuildRootProvider()
    {
        var services = new ServiceCollection();
        ConfigureSharedServices(services);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static void ConfigureSharedServices(IServiceCollection services)
    {
        services.AddSingleton<LambdaRuntimeMarker>();
        LambdaServiceRegistry.Configure<TRequest, TCommand, TResponse>(services);
    }

    private sealed class LambdaRuntimeMarker;

    private static class LambdaServiceRegistry
    {
        private static Action<IServiceCollection>? _configure;

        public static void Register(Action<IServiceCollection> configure) => _configure = configure;

        public static void Configure<TReq, TCmd, TRes>(IServiceCollection services)
            => _configure?.Invoke(services);
    }

    protected static void RegisterServices(Action<IServiceCollection> configure)
        => LambdaServiceRegistry.Register(configure);
}