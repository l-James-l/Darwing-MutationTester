using Core.Startup;
using Microsoft.Extensions.DependencyInjection;

public class CliDependencyRegistrar : DependencyRegistrar
{
    public CliDependencyRegistrar(IServiceCollection serviceCollection) : base(serviceCollection) { }

    protected override void RegisterLocalDependencies()
    {
        Services.AddSingleton<CLIApp>();
    }
}
