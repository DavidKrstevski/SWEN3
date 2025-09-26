using Microsoft.EntityFrameworkCore;
using Paperless_API.Data;
using Paperless_API.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<PaperlessDbContext>(o => o.UseNpgsql(cs));
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseAuthorization();
app.UseCors("AllowAll");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaperlessDbContext>();
    db.Database.Migrate();
}

app.Run();
