using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paperless_AI;
using RabbitMQ;
using System.Reflection.Metadata;

namespace GenAIWorker;

public class GenAiWorkerService : BackgroundService
{
    private readonly RabbitMqConsumer _consumer;
    private readonly GeminiClient _gemini;
    private readonly SummaryStorage _storage;
    private readonly ILogger<GenAiWorkerService> _logger;
    private readonly IRabbitMqProducer _producer;

    public GenAiWorkerService(RabbitMqConsumer consumer, GeminiClient gemini, SummaryStorage storage, ILogger<GenAiWorkerService> logger, IRabbitMqProducer producer)
    {
        _consumer = consumer;
        _gemini = gemini;
        _storage = storage;
        _logger = logger;
        _producer = producer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GenAI worker started");

        var queue1 = "ocr_completed";
        var queue2 = "ai_chat_requests";

        return _consumer.ConsumeManyAsync(
            new[] { queue1, queue2 },
            async (queue, message) =>
            {
                if (queue == queue1)
                {
                    await HandleOcrCompleted(message, stoppingToken);
                }
                else if (queue == queue2)
                {
                    await HandleOtherJob(message, stoppingToken);
                }
            },
            stoppingToken);
    }

    private async Task HandleOcrCompleted(AICompletedMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Received OCR completed message for DocumentId {DocumentId}", message.Id);

            //OCR-Text holen
            string ocrText = await _storage.LoadOcrTextAsync(message, ct);

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("Empty OCR text for DocumentId {DocumentId}", message.Id);
                return;
            }

            //Gemini aufrufen
            var summary = await _gemini.SummarizeAsync(ocrText, ct);
            var legalCheck = await _gemini.LegalCheckAsync(ocrText, ct);
            AICompletedMessage returnMessage = new AICompletedMessage
            {
                Id = message.Id,
                FileName = message.FileName,
                Size = message.Size,
                UploadDate = message.UploadDate,
            };
            returnMessage.ChatHistory = legalCheck.Answer;
            returnMessage.RiskColor = legalCheck.TrafficLight;
            returnMessage.Summary = summary;
            await _producer.PublishAsync(returnMessage, _producer.Host, "ocr_completed_legal");

            if (string.IsNullOrWhiteSpace(summary))
            {
                _logger.LogWarning("Gemini returned empty summary for DocumentId {DocumentId}", message.Id);
                return;
            }

            var objectKey = await _storage.StoreSummaryAsync(message.Id, summary, ct);


            _logger.LogInformation("Summary stored for DocumentId {DocumentId} as {ObjectKey}",
                message.Id, objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing DocumentId {DocumentId}", message.Id);
        }
    }

    private async Task HandleOtherJob(AICompletedMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Received Ask AI {DocumentId}", message.Id);

            //OCR-Text holen
            string ocrText = await _storage.LoadOcrTextAsync(message, ct);

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("Empty OCR text for DocumentId {DocumentId}", message.Id);
                return;
            }

            var messages = message.ChatHistory.Split("---ENTER---");

            //Gemini aufrufen
            var answer = await _gemini.AskGeminiAndPrintAsync(ocrText, messages[messages.Length-1], ct);
            message.ChatHistory += $"\n---ENTER---\n {answer}";
            await _producer.PublishAsync(message, _producer.Host, "ocr_completed_legal");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing DocumentId {DocumentId}", message.Id);
        }
    }
}
