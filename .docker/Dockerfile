# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Add NuGet source for Telegram.Bot
RUN dotnet nuget add source https://pkgs.dev.azure.com/tgbots/Telegram.Bot/_packaging/release/nuget/v3/index.json -n Telegram.Bot

# Copy the solution file and restore dependencies
COPY InTheOfficeBot.sln ./
COPY InTheOfficeBot/InTheOfficeBot.csproj ./InTheOfficeBot/
RUN dotnet restore ./InTheOfficeBot/InTheOfficeBot.csproj

# Copy the remaining project files and build the app
COPY InTheOfficeBot/. ./InTheOfficeBot/
WORKDIR /app/InTheOfficeBot
RUN dotnet publish -c Release -o out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install required globalization packages (if needed)
RUN apt-get update && apt-get install -y \
    icu-devtools \
    && rm -rf /var/lib/apt/lists/*

# Set environment variable for the runtime environment
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy the published files from the build stage
COPY --from=build-env /app/InTheOfficeBot/out .

# Expose port 80 for the application
EXPOSE 80

# Set the URLs the app should listen on (optional but recommended)
ENV ASPNETCORE_URLS=http://+:80

# Entry point for the app
ENTRYPOINT ["dotnet", "InTheOfficeBot.dll"]