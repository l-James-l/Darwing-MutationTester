using Core.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace CLI;

public class CliDependencyRegistrar : DependencyRegistrar
{
    public CliDependencyRegistrar(IServiceCollection serviceCollection) : base(serviceCollection) { }

    protected override void RegisterLocalDependencies()
    {
        Services.AddSingleton<CLIApp>();
    }
}