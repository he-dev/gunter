using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Dapper;
using Gunter.ComponentSetup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reusable;
using Reusable.Data.Repositories;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachements;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.sdk.Mailr;
using Reusable.Utilities.SqlClient;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;

namespace Gunter.Tests
{
    using static ProgramContainerFactory;
    using static  Assert;

    [TestClass]
    public class UseCaseTest
    {
        private MemoryRx _memoryRx;
        private ILoggerFactory _loggerFactory;

        [TestInitialize]
        public async Task TestInitialize()
        {
            _loggerFactory =
                new LoggerFactory()
                    .AttachObject("Environment", System.Configuration.ConfigurationManager.AppSettings["app:Environment"])
                    .AttachObject("Product", ProgramInfo.FullName)
                    .AttachScope()
                    .AttachSnapshot()
                    .Attach<Timestamp<DateTimeUtc>>()
                    .AttachElapsedMilliseconds()
                    .AddObserver(_memoryRx = new MemoryRx());

            var sql = EmbeddedFileProvider<UseCaseTest>.Default.ReadTextFile(@"sql\populate-test-data.sql");
            
            // todo - fix this
            //var sql = sqlFile.DeserializeAsync<string>().GetAwaiter().GetResult();
            var connectionString = ConnectionStringRepository.Default.GetConnectionString("name=TestDb");
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql);
            }
        }

        [TestMethod]
        public async Task CanSendAlert()
        {
            var mailrClient = Mock.Create<IRestClient<IMailrClient>>();

            mailrClient
                .Arrange(x => x.InvokeAsync<string>(Arg.IsAny<HttpMethodContext>(), Arg.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(string.Empty))
                .OccursOnce();

            using (var program = Program.Create(_loggerFactory, builder =>
            {
                builder.RegisterInstance(mailrClient).As<IRestClient<IMailrClient>>();
            }))
            {
                await program.RunAsync(@"cfg\tests\ok");

                var exceptions = _memoryRx.Exceptions<Exception>();

                IsFalse(exceptions.Any());
                mailrClient.Assert();
            }
        }
    }

    internal static class LoggerExtensions
    {
        public static IEnumerable<T> Exceptions<T>(this IEnumerable<ILog> logs) where T : Exception
        {
            return 
                logs
                    .Select(log => log.Exception<T>())
                    .Where(Conditional.IsNotNull);
        }
    }

    internal static class LogExtensions
    {
        public static T Exception<T>(this ILog log) where T : Exception => log.Property<T>();

        //public static T PropertyOrDefault<T>(this ILog log, string name) => log.TryGetValue(name, out var value) && value is T actual ? actual : default;
    }
}
