using Microsoft.EntityFrameworkCore;
using Paperless_AccessBatch.DB;
using Paperless_AccessBatch.Services;
using Paperless_AccessBatch.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AccessBatchDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"));
});

builder.Services.AddScoped<AccessLogXmlParser>();
builder.Services.AddScoped<AccessLogPersistenceService>();

builder.Services.AddHostedService<AccessLogBatchWorker>();

var host = builder.Build();
host.Run();