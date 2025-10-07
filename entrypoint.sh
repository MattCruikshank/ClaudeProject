#!/bin/bash
set -e

# Check if required environment variables are set
if [ -z "$TS_AUTHKEY" ]; then
    echo "ERROR: TS_AUTHKEY environment variable is required"
    exit 1
fi

if [ -z "$TS_HOSTNAME" ]; then
    echo "ERROR: TS_HOSTNAME environment variable is required"
    exit 1
fi

echo "Starting Tailscale daemon..."
tailscaled --state=/var/lib/tailscale/tailscaled.state --socket=/var/run/tailscale/tailscaled.sock &

# Wait for tailscaled to start
sleep 2

echo "Authenticating with Tailscale as ${TS_HOSTNAME}..."
tailscale up --authkey="${TS_AUTHKEY}" --hostname="${TS_HOSTNAME}" --accept-routes

echo "Tailscale is up! Status:"
tailscale status

echo "Starting application in background..."
gosu appuser dotnet Tailmail.Web.dll "$@" &
APP_PID=$!

# Trap signals and forward them to the app
trap "kill -TERM $APP_PID 2>/dev/null" SIGTERM SIGINT

echo "Waiting for application to start..."
sleep 20

echo "Configuring Tailscale serve..."
tailscale serve --bg --set-path=/ http://localhost:8080
tailscale serve --bg --https=8081 --set-path=/ http://localhost:8081

echo "Setup complete! Waiting for application..."
wait $APP_PID
