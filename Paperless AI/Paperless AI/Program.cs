using GenAIWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddLogging();

        services.AddHttpClient<GeminiClient>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        });

        services.AddSingleton<RabbitMqConsumer>();
        services.AddSingleton<SummaryStorage>();  // MinIO
        services.AddHostedService<GenAiWorkerService>();
    })
    .Build();

await host.RunAsync();
