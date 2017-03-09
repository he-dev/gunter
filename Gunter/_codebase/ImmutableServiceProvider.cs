using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlitchDetector
{
    public interface IImmutableServiceProvider
    {        
        T GetService<T>();
        IImmutableServiceProvider Add<T>(T service);
    }

    public class ImmutableServiceProvider : IImmutableServiceProvider
    {
        private readonly IImmutableDictionary<Type, object> _services;

        private ImmutableServiceProvider() => _services = ImmutableDictionary<Type, object>.Empty;

        private ImmutableServiceProvider(IImmutableDictionary<Type, object> services) => _services = services;

        public static ImmutableServiceProvider Empty => new ImmutableServiceProvider();

        public T GetService<T>() => _services.TryGetValue(typeof(T), out object service) ? (T)service : default(T);

        public IImmutableServiceProvider Add<T>(T service) => new ImmutableServiceProvider(_services.Add(typeof(T), service));

        public static IImmutableServiceProvider Create(params object[] services) => new ImmutableServiceProvider(services.ToImmutableDictionary(x => x.GetType()));
    }
}
