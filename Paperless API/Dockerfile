# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Paperless API/Paperless API.csproj", "app/"]
RUN dotnet restore "app/Paperless API.csproj"
COPY ["Paperless API/", "app/"]
WORKDIR /src/app
RUN dotnet publish "Paperless API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Paperless API.dll"]
