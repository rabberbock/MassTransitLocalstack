using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundTasks
{
    public class MessageEventsBackgroundService : BackgroundService
    {
        private readonly IBusControl _busControl;
        private readonly ILogger<MessageEventsBackgroundService> _logger;

        public MessageEventsBackgroundService(IBusControl busControl, ILogger<MessageEventsBackgroundService> logger)
        {
            _busControl = busControl;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting the bus...");
                await _busControl.StartAsync(stoppingToken);

                _logger.LogInformation($"Starting service {nameof(MessageEventsBackgroundService)}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    // Keeps the service alive until it is cancelled.

                    await _busControl.Publish(new SampleMessage { Message = "Hello World"}, stoppingToken);


                    Thread.Sleep(10_000);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in {nameof(MessageEventsBackgroundService)}. Exception {e}");
            }
            finally
            {
                _logger.LogInformation("Stopping the bus...");
                await _busControl.StopAsync(stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping the bus...");
            return _busControl.StopAsync(cancellationToken);
        }
    }
}
