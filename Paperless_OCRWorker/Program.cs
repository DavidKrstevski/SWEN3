using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "rabbitmq" };

IConnection? connection = null;
IChannel? channel = null;

for (int i = 1; i <= 10; i++) // try up to 10 times
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
        await Task.Delay(5000); // wait 5 seconds before retry
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

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"Received message: {message}");
    await Task.CompletedTask;
};

await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
await Task.Delay(Timeout.Infinite);
