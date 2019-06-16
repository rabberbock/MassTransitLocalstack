

using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MassTransit;
using MassTransit.AmazonSqsTransport.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundTasks
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(Environment.CurrentDirectory);
                    config.AddJsonFile("appsettings.json", optional: false);
                    config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json"
                        , optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostBuilder, services) =>
                    {
                        var configuration = hostBuilder.Configuration;
                  
                        services.AddScoped<IConsumer<SampleMessage>, SampleMessageHandler>();
                        services.AddScoped<SampleMessageHandler>();

                        services.AddMassTransit(cfg =>
                        {
                            var config = new EventBusSettings
                            {
                                AccessKey = "test",
                                SecretKey = "test",
                                Region = "us-east-1",
                                SnsConfig = new AmazonSimpleNotificationServiceConfig
                                {
                                    ServiceURL = "http://docker.localhost:4575",
                                },
                                SqsConfig = new AmazonSQSConfig
                                {
                                    ServiceURL = "http://docker.localhost:4576"
                                },
                                PublishOnlyBus = false,
                            };

                            cfg.AddBus(provider => Bus.Factory.CreateUsingAmazonSqs(sbc =>
                            {
                                sbc.UseExtensionsLogging(services.BuildServiceProvider().GetRequiredService<ILoggerFactory>());
                                sbc.DeployTopologyOnly = config.PublishOnlyBus;

                                var host = sbc.Host(config.Region, h =>
                                {
                                    if (config.SqsConfig != null)
                                    {
                                        h.Config(config.SqsConfig);
                                    }

                                    if (config.SnsConfig != null)
                                    {
                                        h.Config(config.SnsConfig);
                                    }

                                    h.AccessKey(config.AccessKey);
                                    h.SecretKey(config.SecretKey);
                            
                                });

                                sbc.OverrideDefaultBusEndpointQueueName("TempMassTransitQueue");
                                sbc.AutoDelete = false;
                                sbc.ReceiveEndpoint(host,"my_queue", c =>
                                {
                                    c.Consumer<SampleMessageHandler>(provider);
                                });
                            }));

                            services.AddSingleton<IHostedService, MessageEventsBackgroundService>();
                        });

                    }
                );
    }

    public class EventBusSettings
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public AmazonSimpleNotificationServiceConfig SnsConfig { get; set; }
        public AmazonSQSConfig SqsConfig { get; set; }
        public bool PublishOnlyBus { get; set; }
    }

    public class SampleMessageHandler : IConsumer<SampleMessage>
    {
        private readonly ILogger<SampleMessageHandler> _logger;
        public SampleMessageHandler(ILogger<SampleMessageHandler> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<SampleMessage> context)
        {
            _logger.LogInformation($"{nameof(SampleMessageHandler)} being called with {context.Message.Message}");
            return Task.CompletedTask;
        }
    }

    public class SampleMessage
    {
        public string Message { get; set; }
    }
}
