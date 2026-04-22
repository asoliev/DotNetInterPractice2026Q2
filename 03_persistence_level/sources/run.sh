#!/usr/bin/env bash
# Run this script from the sources/ folder to set up and run the Ticketing demo.
# Usage: bash run.sh

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

export PATH="$PATH:$HOME/.dotnet/tools"

APP_PROJECT="TicketingSystem.Application/TicketingSystem.Application.csproj"
EF_PROJECT="TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj"

# Install dotnet-ef if missing
if ! dotnet ef --version &>/dev/null 2>&1; then
  echo "Installing dotnet-ef tool..."
  dotnet tool install --global dotnet-ef
fi

# Apply migrations (creates ticketing.db if absent)
echo "Applying EF migrations..."
dotnet ef database update \
  --project "$EF_PROJECT" \
  --startup-project "$APP_PROJECT"

# Run the demo app
echo ""
dotnet restore "$APP_PROJECT"
dotnet build "$APP_PROJECT" --configuration Debug
dotnet TicketingSystem.Application/bin/Debug/net10.0/TicketingSystem.Application.dll
