using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Module = Autofac.Module;

namespace Gunter.AutofacModules
{
    internal class ReportingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Report>()
                .As<IReport>();

            builder
                .RegisterType<TestCaseInfo>();

            builder
                .RegisterType<DataSourceInfo>();

            builder
                .RegisterType<DataSummary>();
        }
    }
}
