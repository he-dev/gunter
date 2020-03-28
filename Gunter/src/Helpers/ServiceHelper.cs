using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;

namespace Gunter.Helpers
{
    public static class ServiceHelper
    {
        public static async Task<T> ExecuteAsync<T>(this IComponentContext componentContext, Type serviceType, params object[] arguments)
        {
            var service = componentContext.GetService(serviceType, arguments);
            var execute = service.GetType().GetExecuteMethod(arguments);

            return await (Task<T>)execute.Invoke(service, arguments);
        }
        
        public static async Task ExecuteAsync(this IComponentContext componentContext, Type serviceType, params object[] arguments)
        {
            var service = componentContext.GetService(serviceType, arguments);
            var execute = service.GetType().GetExecuteMethod(arguments);

            await (Task)execute.Invoke(service, arguments);
        }

        public static T Execute<T>(this IComponentContext componentContext, Type serviceType, params object[] arguments)
        {
            var service = componentContext.GetService(serviceType, arguments);
            var execute = service.GetType().GetExecuteMethod(arguments);

            return (T)execute.Invoke(service, arguments);
        }

        private static object GetService(this IComponentContext componentContext, Type serviceType, IEnumerable<object> arguments)
        {
            var concreteServiceType = serviceType.MakeGenericType(arguments.Select(a => a.GetType()).ToArray());
            return componentContext.Resolve(concreteServiceType);
        }

        private static MethodInfo GetExecuteMethod(this IReflect serviceType, IEnumerable<object> arguments)
        {
            // Get only methods where parameters match exactly the arguments. There should be only one such method.
            var executeMethods =
                from m in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                let parameters = m.GetParameters()
                let parameterTypes = m.GetParameters().Select(p => p.ParameterType)
                let parameterMatches = arguments.Zip(parameterTypes, (a, t) => t.IsInstanceOfType(a))
                where parameterMatches.Count(s => s) == arguments.Count()
                select m;

            return executeMethods.Single();
        }
    }
}