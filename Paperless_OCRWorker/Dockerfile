# ------------------------------
# Build stage
# ------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# copy the rest of the code and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ------------------------------
# Runtime stage
# ------------------------------
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Paperless_OCRWorker.dll"]
