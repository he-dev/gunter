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
using Reusable.OmniLog.Abstractions;
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
    using static ContainerFactory;
    using static Assert;

    public class UseCaseTest : IAsyncLifetime, IClassFixture<TeapotFactoryFixture>
    {
        private const string Url = "http://localhost:50123";

        private static readonly IResourceProvider Sql = EmbeddedFileProvider<UseCaseTest>.Default.DecorateWith(RelativeProvider.Factory("sql"));

        private readonly TeapotServer _teapot;
        private readonly MemoryRx _memoryRx;
        private readonly ILoggerFactory _loggerFactory;

        private readonly IDisposable _cleanUp;

        public UseCaseTest(TeapotFactoryFixture teapotFactory)
        {
            _teapot = teapotFactory.CreateTeapotServer(Url);

            _loggerFactory =
                LoggerFactory
                    .Empty
                    .AttachObject("Environment", ConfigurationManager.AppSettings["app:Environment"])
                    .AttachObject("Product", ProgramInfo.FullName)
                    .AttachScope()
                    .AttachSnapshot()
                    .Attach<Timestamp<DateTimeUtc>>()
                    .AttachElapsedMilliseconds()
                    .AddObserver(_memoryRx = new MemoryRx());

            _cleanUp = Disposable.Create(() =>
            {
                _loggerFactory.Dispose();
                _teapot.Dispose();
            });
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
                                .Value("$.Subject")
                                .IsNotNull()
                                .IsEqual("Glitch alert [Fatal]");
                        })
                        .Occurs(1);

                    response
                        .Once(200, "OK");
                });

                using (var program = Program.Create(_loggerFactory, CustomizeContainer))
                {
                    await program.RunAsync(@"run -files example -tests fails");
                    //await Task.Delay(300);

                    _memoryRx.Exceptions<Exception>().AssertNone();

                    //False(exceptions.Any(), "There must not occur any exceptions.");

                    testResult.Assert();
                }

                void CustomizeContainer(ContainerBuilder builder)
                {
                    builder
                        .RegisterInstance((ExecuteExceptionCallback)(ex => throw ex));
                }
            }
        }

        [Fact]
        public async Task Sends_alert_when_test_passes()
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
                                .Value("$.Subject")
                                .IsEqual("Glitch alert [Information]");
                        })
                        .Occurs(1);

                    response
                        .Once(200, "OK");
                });

                using (var program = Program.Create(_loggerFactory, builder =>
                {
                    builder
                        .RegisterInstance((ExecuteExceptionCallback)(ex => throw ex));
                }))
                {
                    await program.RunAsync(@"run -files example -tests passes");

                    var exceptions = _memoryRx.Exceptions<Exception>();

                    False(exceptions.Any(), "There must not occur any exceptions.");

                    testResult.Assert();
                }
            }
        }

        public Task DisposeAsync()
        {
            _cleanUp.Dispose();
            return Task.CompletedTask;
        }
    }
}