using Microsoft.Extensions.DependencyInjection;
using Mutator.MutationImplementations;
using NSubstitute;

namespace CoreTests.Startup;

public abstract class DepencyRegisrationTestsHelper
{
    protected int _expectedRegistrations;
    protected IServiceCollection? _services { get; set; }

    protected void AssertMutatorRegistration<T>() => AssertBasicRegistartion<IMutationImplementation, T>(true);

    protected void AssertBasicRegistartion<T>(bool isSingleton = true) => AssertBasicRegistartion<T, T>(isSingleton);

    protected void AssertBasicRegistartion<T1, T2>(bool isSingleton = true)
    {
        _expectedRegistrations++;

        if (_services is null)
        {
            Assert.Fail(nameof(_services) + " not instantiated.");
            return;
        }

        _services.Received(1).Add(Arg.Is<ServiceDescriptor>(x =>
        x.Lifetime == (isSingleton ? ServiceLifetime.Singleton : ServiceLifetime.Transient)
        && x.ImplementationType == typeof(T2)
        && x.ServiceType == typeof(T1)));
    }

    protected void AssertRegisterManySingleton<T>(Type[] baseTypes)
    {
        if (_services is null)
        {
            Assert.Fail(nameof(_services) + " not instantiated.");
            return;
        }

        AssertBasicRegistartion<T>();
        foreach (Type type in baseTypes)
        {
            _expectedRegistrations++;

            _services.Received().Add(Arg.Is<ServiceDescriptor>(x =>
            x.Lifetime == ServiceLifetime.Singleton
            && x.ServiceType == type
            && x.ImplementationFactory != null));
        }
        //TODO: Further validate the implementation factory creates the correct instance. Dont currently know how to do this.
        //This also means that where multiple classes are registered against the same class, cant assert this.
    }
}