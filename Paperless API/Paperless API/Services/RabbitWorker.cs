using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paperless_API.Data.Repositories;
using Paperless_API.Entities;
using Paperless_API.Messaging;

namespace Paperless_API.Services
{
    public class RabbitWorker : BackgroundService
    {
        private readonly RabbitMqConsumer _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitWorker> _logger;

        public RabbitWorker(RabbitMqConsumer consumer, IServiceScopeFactory scopeFactory, ILogger<RabbitWorker> logger)
        {
            _consumer = consumer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _consumer.ConsumeAsync(async (Document incoming) =>
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                var existing = await repo.GetAsync(incoming.Id, stoppingToken);

                incoming.FileName = string.IsNullOrWhiteSpace(incoming.FileName) ? existing.FileName : incoming.FileName;
                incoming.Size = incoming.Size == 0 ? existing.Size : incoming.Size;
                incoming.UploadDate = incoming.UploadDate == default ? existing.UploadDate : incoming.UploadDate;

                incoming.Summary ??= existing.Summary;
                incoming.ChatHistory ??= existing.ChatHistory;
                incoming.RiskColor ??= existing.RiskColor;

                await repo.UpdateAsync(incoming, stoppingToken);

                _logger.LogInformation("Updated document {Id} from RabbitMQ", incoming.Id);
            }, stoppingToken);
        }
    }
}