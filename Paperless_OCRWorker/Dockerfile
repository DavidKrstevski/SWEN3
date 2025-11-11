FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        tesseract-ocr \
        poppler-utils \
        ghostscript \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV LANG=C.UTF-8

ENTRYPOINT ["dotnet", "Paperless_OCRWorker.dll"]
