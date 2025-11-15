using Paperless_OCRWorker.Messaging;

namespace Paperless_OCRWorker.Services;

public class OcrService
{
    private readonly MinioService _minio;
    private readonly IRabbitMqProducer _producer;

    public OcrService(MinioService minio, IRabbitMqProducer producer)
    {
        _minio = minio;
        _producer = producer;
    }

    public async Task ProcessDocumentAsync(DocumentMessage doc)
    {
        try
        {
            var fileName = $"{doc.Id}.pdf";
            var localPdf = Path.Combine("/tmp", fileName);
            var localTxt = Path.Combine("/tmp", $"{doc.Id}.txt");

            await _minio.EnsureBucketExistsAsync();
            await _minio.DownloadFileAsync(fileName, localPdf);

            Console.WriteLine($"Processing {fileName}...");

            bool textExtracted = await PdfTextExtractor.ExtractTextAsync(localPdf, localTxt);
            if (!textExtracted)
            {
                Console.WriteLine("pdftotext failed, running Tesseract...");
                await PdfTextExtractor.RunTesseractAsync(localPdf, localTxt);
            }

            if (File.Exists(localTxt))
            {
                await _minio.UploadFileAsync($"{doc.Id}.txt", localTxt);
                Console.WriteLine($"Uploaded OCR result: {doc.Id}.txt");
                await _producer.PublishAsync(doc, _producer.Host, _producer.Queue);
            }
            else
            {
                Console.WriteLine("No OCR result found.");
            }

            File.Delete(localPdf);
            if (File.Exists(localTxt)) File.Delete(localTxt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing document {doc.FileName}: {ex.Message}");
        }
    }
}
