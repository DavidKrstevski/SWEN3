using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenAIWorker;

public class GenAiWorkerService : BackgroundService
{
    private readonly RabbitMqConsumer _consumer;
    private readonly GeminiClient _gemini;
    private readonly SummaryStorage _storage;
    private readonly ILogger<GenAiWorkerService> _logger;

    public GenAiWorkerService(
        RabbitMqConsumer consumer,
        GeminiClient gemini,
        SummaryStorage storage,
        ILogger<GenAiWorkerService> logger)
    {
        _consumer = consumer;
        _gemini = gemini;
        _storage = storage;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GenAI worker started");

        // WICHTIG: Nicht fire-and-forget, sondern Task zurückgeben
        return _consumer.ConsumeAsync(async message =>
        {
            try
            {
                _logger.LogInformation("Received OCR completed message for DocumentId {DocumentId}", message.Id);

                // 1. OCR-Text holen (direkt aus Message oder aus MinIO)
                string ocrText = await _storage.LoadOcrTextAsync(message, stoppingToken);

                if (string.IsNullOrWhiteSpace(ocrText))
                {
                    _logger.LogWarning("Empty OCR text for DocumentId {DocumentId}", message.Id);
                    return;
                }

                // 2. Gemini aufrufen
                var summary = await _gemini.SummarizeAsync(ocrText, stoppingToken);

                if (string.IsNullOrWhiteSpace(summary))
                {
                    _logger.LogWarning("Gemini returned empty summary for DocumentId {DocumentId}", message.Id);
                    return;
                }

                // 3. Summary in MinIO speichern
                if (!Guid.TryParse(message.Id, out var documentId))
                {
                    _logger.LogError("Invalid Guid in message.Id: {Id}", message.Id);
                    return; // oder throw, je nach Strategie
                }

                var objectKey = await _storage.StoreSummaryAsync(documentId, summary, stoppingToken);


                _logger.LogInformation("Summary stored for DocumentId {DocumentId} as {ObjectKey}",
                    message.Id, objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing DocumentId {DocumentId}", message.Id);
            }

        }, stoppingToken);
    }
}
