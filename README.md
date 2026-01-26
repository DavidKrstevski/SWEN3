Paperless Project



A document management and access tracking system with AI integration and batch processing.





Features



Upload and store documents in MinIO.



Track access logs from external systems via daily XML batch processing.



AI-assisted document chat history and risk analysis.



RabbitMQ messaging for asynchronous updates and AI requests.



PostgreSQL database for persistent storage.





Project Structure



Paperless\_API – REST API for document management and AI interactions.



Paperless\_AccessBatch – Batch worker that processes daily XML access logs.



Workers – Background services for messaging and batch processing.



Entities / Data – EF Core models and repositories.





Setup



Clone the repository:



git clone <repo-url>

cd Paperless





Configure appsettings.json or environment variables for:



PostgreSQL (Postgres connection string)



MinIO (Minio:Endpoint, AccessKey, SecretKey, Bucket)



RabbitMQ (RabbitMQ:Host, User, Pass, Queues)



Access batch folders (AccessBatch:InputFolder, ArchiveFolder)



Install .NET 8 SDK.



Apply EF Core migrations:



dotnet ef migrations add InitialCreate -p Paperless\_API/Paperless\_API.csproj -s Paperless\_API/Paperless\_API.csproj

dotnet ef database update -p Paperless\_API/Paperless\_API.csproj -s Paperless\_API/Paperless\_API.csproj



Running



API



dotnet run --project Paperless\_API/Paperless\_API.csproj





Swagger available at /swagger



CORS enabled for all origins.



Access Batch Worker



dotnet run --project Paperless\_AccessBatch/Paperless\_AccessBatch.csproj





Processes XML files daily (01:00 UTC by default).



Moves processed files to archive folder.



XML Access Log Format

<AccessLogs date="YYYY-MM-DD">

&nbsp;   <Document>

&nbsp;       <DocumentId>GUID</DocumentId>

&nbsp;       <AccessCount>INTEGER</AccessCount>

&nbsp;   </Document>

&nbsp;   <!-- Repeat Document nodes -->

</AccessLogs>



Testing



Unit and integration tests use NUnit.





Run tests:



dotnet test Paperless\_AccessBatch/Tests





Notes



Ensure RabbitMQ and MinIO services are running before uploading documents.



All services support configuration via environment variables for containerized deployments.

