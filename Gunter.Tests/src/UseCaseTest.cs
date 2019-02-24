using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Dapper;
using Gunter.DependencyInjection;
using Gunter.Tests.Helpers;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Data.Repositories;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachments;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Teapot;
using Reusable.Utilities.SqlClient;
using Reusable.Utilities.XUnit.Fixtures;
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

        private static readonly IResourceProvider Sql = EmbeddedFileProvider<UseCaseTest>.Default.DecorateWith(RelativeProvider.Factory("sql"));

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
            var connectionString = ConnectionStringRepository.Default.GetConnectionString("name=TestDb");
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(Sql.ReadTextFile("seed-test-data.sql"));
            }
        }

        [Fact]
        public async Task Sends_alert_when_test_fails()
        {
            using (var teacup = _teapot.BeginScope())
            {
                var testResult = teacup.Mock("/api/v1.0/Gunter/Alerts/TestResult").ArrangePost((request, response) =>
                {
                    request
                        .AsUserAgent(ProgramInfo.Name, ProgramInfo.Version)
                        .AcceptsHtml()
                        .WithContentTypeJson(body =>
                        {
                            body
                                .PropertyEquals("$.Subject", "Glitch alert [Fatal]")
                                .HasProperty("$.Subject");
                        })
                        .Occurs(1);

                    response
                        .Once(200, "OK");
                });

                using (var program = Program.Create(_loggerFactory, builder =>
                {
                    builder
                        .RegisterInstance((ExecuteExceptionCallback) (ex => throw ex));
                }))
                {
                    await program.RunAsync(@"run -files example -tests any");
                    //await Task.Delay(300);

                    var exceptions = _memoryRx.Exceptions<Exception>();

                    False(exceptions.Any(), "There must not occur any exceptions.");

                    testResult.Assert();
                }
            }
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}