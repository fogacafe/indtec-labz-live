using Microsoft.Extensions.DependencyInjection;

namespace Indtec.Labz.Live.Lambda.Core;

public static class LambdaBootstrapper
{
    public static IServiceProvider Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider(validateScopes: true);
    }
}