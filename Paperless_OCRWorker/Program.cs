using System.Text;
using Minio;
using Minio.DataModel.Args;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text.Json;

var minioEndpoint = "minio:9000";
var accessKey = "minioadmin";
var secretKey = "minioadmin";
var bucketName = "documents";

var factory = new ConnectionFactory { HostName = "rabbitmq" };

IConnection? connection = null;
IChannel? channel = null;

for (int i = 1; i <= 10; i++)
{
    try
    {
        Console.WriteLine($"Attempt {i}: Connecting to RabbitMQ...");
        connection = await factory.CreateConnectionAsync("OcrWorker");
        channel = await connection.CreateChannelAsync();
        Console.WriteLine("Connected to RabbitMQ");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Connection attempt {i} failed: {ex.Message}");
        await Task.Delay(5000);
    }
}

if (connection == null || channel == null)
{
    Console.WriteLine("Failed to connect to RabbitMQ after multiple attempts. Exiting.");
    return;
}

var queueName = "ocr_jobs";
await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false);
Console.WriteLine("OCR Worker is listening for messages...");

var minio = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(accessKey, secretKey)
    .WithSSL(false)
    .Build();

async Task EnsureBucketExistsAsync()
{
    bool found = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
    if (!found)
    {
        await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        Console.WriteLine($"Created bucket '{bucketName}'");
    }
}

async Task ProcessMessageAsync(string message)
{
    try
    {
        Console.WriteLine($"Processing message: {message}");

        var doc = JsonSerializer.Deserialize<DocumentMessage>(message);
        if (doc == null || string.IsNullOrWhiteSpace(doc.FileName))
        {
            Console.WriteLine("Invalid message received — missing FileName");
            return;
        }

        var fileName = $"{doc.Id}.pdf";
        var localPdfPath = Path.Combine("/tmp", fileName);
        var ocrTextPath = Path.Combine("/tmp", $"{Path.GetFileNameWithoutExtension(fileName)}.txt");

        await EnsureBucketExistsAsync();

        Console.WriteLine($"Downloading {fileName} from MinIO...");
        await minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(fileName)
            .WithFile(localPdfPath));

        Console.WriteLine("Running OCR with Tesseract...");
        var tesseract = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = $"{localPdfPath} {ocrTextPath.Replace(".txt", "")} -l eng",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = Process.Start(tesseract);
        if (process != null)
        {
            await process.WaitForExitAsync();
            Console.WriteLine("Tesseract finished processing.");
        }

        if (File.Exists(ocrTextPath))
        {
            using var resultStream = File.OpenRead(ocrTextPath);
            await minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject($"{Path.GetFileNameWithoutExtension(fileName)}.txt")
                .WithStreamData(resultStream)
                .WithObjectSize(resultStream.Length)
                .WithContentType("text/plain"));
            Console.WriteLine($"Uploaded OCR result: {Path.GetFileNameWithoutExtension(fileName)}.txt");
        }
        else
        {
            Console.WriteLine("OCR result file not found.");
        }

        File.Delete(localPdfPath);
        if (File.Exists(ocrTextPath)) File.Delete(ocrTextPath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing OCR job: {ex.Message}");
    }
}


var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"Received message: {message}");
    await ProcessMessageAsync(message);
};

await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
await Task.Delay(Timeout.Infinite);

record DocumentMessage
{
    public string? Id { get; set; }
    public string? FileName { get; set; }
    public long Size { get; set; }
    public DateTimeOffset UploadDate { get; set; }
}
