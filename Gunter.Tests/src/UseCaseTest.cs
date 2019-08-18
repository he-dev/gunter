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
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Rx;
using Reusable.OmniLog.Scalars;
using Reusable.Teapot;
using Reusable.Translucent;
using Reusable.Utilities.SqlClient;
using Reusable.Utilities.XUnit.Fixtures;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;
using Xunit;

namespace Gunter.Tests
{
    using static ContainerFactory;
    using static Assert;

    public class UseCaseTest : IAsyncLifetime, IClassFixture<TeapotServerFixture>
    {
        private const string SrvUrl = "http://localhost:50123";
        private const string ApiUrl = "/api/v2.0/Gunter/Alerts/TestResult";

        private static readonly IResourceRepository Resources =
            ResourceRepository.Create(c => c
                .AddEmbeddedFiles<UseCaseTest>(@"Gunter\Tests\res\sql", @"Gunter\Tests\res\cfg\test")
                .AddJsonFile(ProgramInfo.CurrentDirectory, "appsettings.json")
            );

        private readonly IDisposable _cleanUp;
        private readonly ITeapotServerContext _teacup;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MemoryRx _memoryRx;

        public UseCaseTest(TeapotServerFixture teapotFactory)
        {
            _teacup = teapotFactory.GetServer(SrvUrl).BeginScope();
            _loggerFactory =
                new LoggerFactory()
                    .UseCorrelation()
                    .UseStopwatch()
                    .UseLambda()
                    .UseBuilder()
                    .UseMapper()
                    .UseSerializer()
                    .UseEcho(_memoryRx = new MemoryRx());

            _cleanUp = Disposable.Create(() =>
            {
                _loggerFactory.Dispose();
                _teacup.Dispose();
            });
        }

        public async Task InitializeAsync()
        {
            //var connectionString = ConnectionStringRepository.Default.GetConnectionString("name=TestDb");
//            using (var conn = new SqlConnection(connectionString))
//            {
//                await conn.ExecuteAsync(Resources.ReadTextFile("seed-test-data.sql"));
//            }
        }

        [Fact]
        public async Task Sends_alert_when_test_fails()
        {
            var testResult =
                _teacup
                    .MockPost(ApiUrl, request =>
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
                    })
                    .ArrangeResponse(response => response.Once(200, "OK"));

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
                //builder.RegisterInstance((ExecuteExceptionCallback)(ex => throw ex));
            }
        }

        [Fact]
        public async Task Sends_alert_when_test_passes()
        {
            var testResult = _teacup.MockPost(ApiUrl, request =>
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
            }).ArrangeResponse(response => response.Once(200, "OK"));

            using (var program = Program.Create(_loggerFactory, builder =>
            {
                //builder.RegisterInstance((ExecuteExceptionCallback)(ex => throw ex));
            }))
            {
                await program.RunAsync(@"run -files example -tests passes");

                var exceptions = _memoryRx.Exceptions<Exception>();

                False(exceptions.Any(), "There must not occur any exceptions.");

                testResult.Assert();
            }
        }

        public Task DisposeAsync()
        {
            _cleanUp.Dispose();
            return Task.CompletedTask;
        }
    }
}