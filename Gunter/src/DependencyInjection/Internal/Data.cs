using System;
using Autofac;

namespace Gunter.DependencyInjection.Internal
{
    internal class Data : Autofac.Module
    {
        protected override void Load(Autofac.ContainerBuilder builder)
        {
            builder
                .RegisterType<Gunter.Data.TestBundle>();

            builder
                .RegisterType<Gunter.Data.SqlClient.TableOrView>();

            builder
                .RegisterType<Gunter.Data.TestCase>();
            
            builder
                .RegisterType<Gunter.Data.VariableCollection>();
        }
    }
}