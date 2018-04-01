using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;

namespace Gunter.Messaging.Emails.Internal
{
    [PublicAPI]
    internal interface IModuleFactory
    {
        bool CanCreate([NotNull] IModule module);

        object Create([NotNull] IModule module, [NotNull] TestContext context);
    }

    public abstract class ModuleFactory : IModuleFactory
    {
        public bool CanCreate(IModule module)
        {
            var moduleType = module.GetType();
            return
                GetType()
                    .GetCustomAttribute<ModuleFactoryForAttribute>()
                    .Any(type => type.IsAssignableFrom(moduleType));
        }

        public abstract object Create(IModule module, TestContext context);
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal class ModuleFactoryForAttribute : Attribute, IEnumerable<Type>
    {
        private readonly IEnumerable<Type> _renderables;

        public ModuleFactoryForAttribute(params Type[] renderables) => _renderables = renderables;

        public bool Contains(Type type) => _renderables.Contains(type);

        public IEnumerator<Type> GetEnumerator() => _renderables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}