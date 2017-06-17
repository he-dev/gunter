using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email
{
    [PublicAPI]
    public interface IModuleRenderer
    {
        bool CanRender(IModule module);
        string Render([NotNull] IModule module, [NotNull] TestUnit testUnit, [NotNull] IServiceProvider serviceProvider);
    }

    public abstract class ModuleRenderer : IModuleRenderer
    {
        public bool CanRender(IModule module)
        {
            var moduleType = module.GetType();
            return 
                GetType()
                    .GetCustomAttribute<CanRenderAttribute>()
                    .Any(type => type.IsAssignableFrom(moduleType));
        }

        public abstract string Render(IModule module, TestUnit testUnit, IServiceProvider serviceProvider);       

        protected IMarkupElement Html => MarkupElement.Builder;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CanRenderAttribute : Attribute, IEnumerable<Type>
    {
        private readonly IEnumerable<Type> _renderables;

        public CanRenderAttribute(params  Type[] renderables) => _renderables = renderables;

        public bool Contains(Type type) => _renderables.Contains(type);

        public IEnumerator<Type> GetEnumerator() => _renderables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}