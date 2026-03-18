using Microsoft.Extensions.DependencyInjection;
using Mutator.MutationImplementations;
using NSubstitute;

namespace CoreTests.Startup;

public abstract class DepencyRegisrationTestsHelper
{
    protected int _expectedRegistrations;
    protected IServiceCollection? _services { get; set; }

    protected void AssertBasicRegistration<T>(bool isSingleton = true) => AssertBasicRegistration<T, T>(isSingleton);

    protected void AssertBasicRegistration<T1, T2>(bool isSingleton = true) => AssertBasicRegistration(typeof(T1), typeof(T2), isSingleton);

    protected void AssertBasicRegistration(Type t1, Type t2, bool isSingleton = true)
    {
        _expectedRegistrations++;

        if (_services is null)
        {
            Assert.Fail(nameof(_services) + " not instantiated.");
            return;
        }

        _services.Received(1).Add(Arg.Is<ServiceDescriptor>(x =>
        x.Lifetime == (isSingleton ? ServiceLifetime.Singleton : ServiceLifetime.Transient)
        && x.ImplementationType == t2
        && x.ServiceType == t1));
    }

    protected void AssertRegisterManySingleton<T>(Type[] baseTypes)
    {
        if (_services is null)
        {
            Assert.Fail(nameof(_services) + " not instantiated.");
            return;
        }

        AssertBasicRegistration<T>();
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