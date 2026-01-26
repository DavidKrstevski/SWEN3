FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Paperless_AccessBatch/Paperless_AccessBatch.csproj", "Paperless_AccessBatch/"]
RUN dotnet restore "Paperless_AccessBatch/Paperless_AccessBatch.csproj"

COPY Paperless_AccessBatch/ Paperless_AccessBatch/
WORKDIR /src/Paperless_AccessBatch

RUN dotnet publish -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Paperless_AccessBatch.dll"]
