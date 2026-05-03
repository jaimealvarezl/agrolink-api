# ── Firebase ─────────────────────────────────────────────────────────────────

resource "google_firebase_project" "default" {
  provider   = google-beta
  project    = var.project_id
  depends_on = [google_project_service.apis]
}

# ── Mobile apps ──────────────────────────────────────────────────────────────

resource "google_firebase_apple_app" "ios" {
  provider     = google-beta
  project      = var.project_id
  display_name = "AgroLink iOS"
  bundle_id    = "com.jaimealv994.agrolink"
  depends_on   = [google_firebase_project.default]
}

data "google_firebase_apple_app_config" "ios" {
  provider = google-beta
  app_id   = google_firebase_apple_app.ios.app_id
}

resource "google_firebase_android_app" "android" {
  provider     = google-beta
  project      = var.project_id
  display_name = "AgroLink Android"
  package_name = "com.jaimealv994.agrolink"
  depends_on   = [google_firebase_project.default]
}

data "google_firebase_android_app_config" "android" {
  provider = google-beta
  app_id   = google_firebase_android_app.android.app_id
}

# ── Auth providers ────────────────────────────────────────────────────────────

resource "google_identity_platform_config" "auth" {
  project = var.project_id

  sign_in {
    email {
      enabled           = true
      password_required = true
    }
  }

  depends_on = [google_firebase_project.default]
}

# ── Outputs ───────────────────────────────────────────────────────────────────
# Extract config files after apply:
#   terraform output -raw ios_google_services_plist | base64 -d > ../../../agrolink-app/GoogleService-Info.plist
#   terraform output -raw android_google_services_json | base64 -d > ../../../agrolink-app/google-services.json

output "ios_google_services_plist" {
  description = "Base64-encoded GoogleService-Info.plist for the Expo app"
  value       = data.google_firebase_apple_app_config.ios.config_file_contents
  sensitive   = true
}

output "android_google_services_json" {
  description = "Base64-encoded google-services.json for the Expo app"
  value       = data.google_firebase_android_app_config.android.config_file_contents
  sensitive   = true
}
