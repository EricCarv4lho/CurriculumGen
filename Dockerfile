# Build stage: compile and publish the API.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file alone first so `dotnet restore` stays layer-cached.
COPY CurriculumGenerator/CurriculumGenerator.csproj CurriculumGenerator/
RUN dotnet restore CurriculumGenerator/CurriculumGenerator.csproj

COPY CurriculumGenerator/ CurriculumGenerator/
RUN dotnet publish CurriculumGenerator/CurriculumGenerator.csproj \
    -c Release \
    -o /app/publish

# Runtime stage: smallest image that runs the published app.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render terminates TLS itself and auto-detects the port the container binds.
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "CurriculumGenerator.dll"]
