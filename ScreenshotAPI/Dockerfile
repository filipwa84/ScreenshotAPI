# Use official .NET 9 runtime as base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install dependencies for Chromium, ChromeDriver, and fonts (Debian-based)
RUN apt-get update && apt-get upgrade -y && \
    apt-get install -y --no-install-recommends \
    chromium \
    chromium-driver \
    fonts-noto \
    fonts-noto-cjk \
    fonts-noto-color-emoji \
    fonts-noto-mono \
    libgbm-dev \
    libxkbcommon-x11-0 \
    libatk-bridge2.0-0 \
    libatspi2.0-0 \
    libxcomposite1 \
    libxrandr2 \
    libxi6 \
    libxdamage1 \
    mesa-utils \
    dbus-x11 \
    alsa-utils \
    curl \
    ca-certificates \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Ensure ChromeDriver is accessible in PATH
ENV PATH="/usr/bin/:$PATH"

# Expose port 5000 explicitly
EXPOSE 5000

# Install .NET SDK and build dependencies
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy and build the application
COPY ["ScreenshotAPI.csproj", "./"]
RUN dotnet restore "ScreenshotAPI.csproj"
COPY . .
RUN dotnet publish "ScreenshotAPI.csproj" -c Release -o /app/publish

# Use final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ensure the app runs on port 5000
ENTRYPOINT ["dotnet", "ScreenshotAPI.dll"]
