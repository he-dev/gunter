using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Dapper;
using Gunter.ComponentSetup;
using JetBrains.Annotations;
using Reusable;
using Reusable.Data.Repositories;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachements;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Teapot;
using Reusable.Utilities.SqlClient;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;
using Xunit;

namespace Gunter.Tests
{
    using static ProgramContainerFactory;
    using static Assert;

    public class UseCaseTest : IAsyncLifetime, IClassFixture<TeapotFactoryFixture>
    {
        private const string Url = "http://localhost:50123";

        private readonly TeapotServer _teapot;
        private readonly MemoryRx _memoryRx;
        private readonly ILoggerFactory _loggerFactory;

        public UseCaseTest(TeapotFactoryFixture teapotFactory)
        {
            _teapot = teapotFactory.CreateTeapotServer(Url);

            _loggerFactory =
                new LoggerFactory()
                    .AttachObject("Environment", ConfigurationManager.AppSettings["app:Environment"])
                    .AttachObject("Product", ProgramInfo.FullName)
                    .AttachScope()
                    .AttachSnapshot()
                    .Attach<Timestamp<DateTimeUtc>>()
                    .AttachElapsedMilliseconds()
                    .AddObserver(_memoryRx = new MemoryRx());
        }

        public async Task InitializeAsync()
        {
            var sql = EmbeddedFileProvider<UseCaseTest>.Default.ReadTextFile(@"sql\populate-test-data.sql");
            var connectionString = ConnectionStringRepository.Default.GetConnectionString("name=TestDb");
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql);
            }
        }

        [Fact]
        public async Task Sends_alert_when_test_fails()
        {
            using (var teacup = _teapot.BeginScope())
            {
                var testResult = teacup.Mock("/api/v2.0/Gunter/Alerts/TestResult").ArrangePost((request, response) =>
                {
                    request
                        .AsUserAgent(ProgramInfo.Name, ProgramInfo.Version)
                        .AcceptsHtml()
                        .WithContentTypeJson(body =>
                        {
                            body
                                .PropertyEquals("$.Subject", "Glitch alert [Debug]")
                                .HasProperty("$.Subject");
                        })
                        .Occurs(1);

                    response
                        .Once(200, "OK");
                });

                using (var program = Program.Create(_loggerFactory, builder => { }))
                {
                    await program.RunAsync(@"batch -tests example");
                    //await Task.Delay(300);

                    var exceptions = _memoryRx.Exceptions<Exception>();

                    False(exceptions.Any());

                    testResult.Assert();
                }
            }
        }


        public Task DisposeAsync()
        {
            return Task.CompletedTask;
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

    [UsedImplicitly]
    public class TeapotFactoryFixture : IDisposable
    {
        private readonly ConcurrentDictionary<string, TeapotServer> _servers;

        public TeapotFactoryFixture()
        {
            _servers = new ConcurrentDictionary<string, TeapotServer>();
        }

        public TeapotServer CreateTeapotServer([NotNull] string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            return _servers.GetOrAdd(url, u => new TeapotServer(u));
        }

        public void Dispose()
        {
            foreach (var teapotServer in _servers.Values)
            {
                teapotServer.Dispose();
            }
        }
    }
}
