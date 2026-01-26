using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paperless_AccessBatch.Services;
using Paperless_AccessBatch.Entities;

namespace Paperless_AccessBatch.Workers
{
    public class AccessLogBatchWorker : BackgroundService
    {
        private readonly ILogger<AccessLogBatchWorker> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly string _inputFolder;
        private readonly string _archiveFolder;

        public AccessLogBatchWorker(
            ILogger<AccessLogBatchWorker> logger,
            IConfiguration config,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _config = config;
            _scopeFactory = scopeFactory;

            _inputFolder = _config["AccessBatch:InputFolder"] ?? "data/access-input";
            _archiveFolder = _config["AccessBatch:ArchiveFolder"] ?? "data/access-archive";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AccessLogBatchWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var files = Directory.GetFiles(_inputFolder, "*.xml");
                    foreach (var file in files)
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var parser = scope.ServiceProvider.GetRequiredService<AccessLogXmlParser>();
                        var persistence = scope.ServiceProvider.GetRequiredService<AccessLogPersistenceService>();

                        _logger.LogInformation("Processing XML file: {File}", file);

                        var dto = parser.Parse(file);
                        var accessDateUtc = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

                        var logs = dto.Entries.Select(e => new DocumentAccessLog
                        {
                            DocumentId = e.DocumentId,
                            AccessCount = e.AccessCount,
                            AccessDate = accessDateUtc
                        }).ToList();


                        await persistence.PersistLogsAsync(logs, stoppingToken);

                        // Move processed file to archive
                        var archivePath = Path.Combine(_archiveFolder, Path.GetFileName(file));
                        File.Move(file, archivePath, overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing XML files");
                }

                // Wait until next run (daily at 01:00)
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(1); // 01:00 UTC
                var delay = nextRun - now;

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                _logger.LogInformation("Next batch run in {Delay}", delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
