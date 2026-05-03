#!/usr/bin/env bash
# Extracts Firebase config files from Terraform outputs into the Expo app.
# Run from the iac/ directory after `terraform apply`.

set -euo pipefail

APP_DIR="$(cd "$(dirname "$0")/../../../../agrolink-app" && pwd)"

echo "Extracting Firebase config files to $APP_DIR..."

terraform output -raw ios_google_services_plist | base64 -d > "$APP_DIR/GoogleService-Info.plist"
echo "  GoogleService-Info.plist written"

terraform output -raw android_google_services_json | base64 -d > "$APP_DIR/google-services.json"
echo "  google-services.json written"

echo "Done. Add both files to .gitignore if not already there."
