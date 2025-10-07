# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Tailmail.Web/Tailmail.Web.csproj", "Tailmail.Web/"]
COPY ["Tailmail.Protos/Tailmail.Protos.csproj", "Tailmail.Protos/"]

# Restore dependencies
RUN dotnet restore "Tailmail.Web/Tailmail.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Tailmail.Web"
RUN dotnet build "Tailmail.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Tailmail.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install Tailscale and gosu
RUN apt-get update && apt-get install -y \
    ca-certificates \
    curl \
    iptables \
    iproute2 \
    gosu \
    && curl -fsSL https://tailscale.com/install.sh | sh \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create state directory for Tailscale
RUN mkdir -p /var/run/tailscale /var/cache/tailscale /var/lib/tailscale

# Create non-root user
RUN useradd -m -u 1000 appuser && \
    mkdir -p /app && \
    chown -R appuser:appuser /app

WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=publish --chown=appuser:appuser /app/publish .
COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
