# ── Service Accounts ─────────────────────────────────────────────────────────

resource "google_service_account" "api" {
  account_id   = "agrolink-api"
  display_name = "AgroLink API"
  description  = "Used by the Cloud Run API service"
}

resource "google_service_account" "cicd" {
  account_id   = "agrolink-cicd"
  display_name = "AgroLink CI/CD"
  description  = "Used by GitHub Actions to deploy images and update Cloud Run"
}

# ── API service account permissions ──────────────────────────────────────────

# Read/write GCS files bucket
resource "google_storage_bucket_iam_member" "api_files_rw" {
  bucket = google_storage_bucket.files.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.api.email}"
}

# Access Cloud SQL via Cloud SQL connector
resource "google_project_iam_member" "api_cloudsql" {
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.api.email}"
}

# Read secrets from Secret Manager
resource "google_project_iam_member" "api_secret_accessor" {
  project = var.project_id
  role    = "roles/secretmanager.secretAccessor"
  member  = "serviceAccount:${google_service_account.api.email}"
}

# ── CI/CD service account permissions ────────────────────────────────────────

# Push Docker images to Artifact Registry
resource "google_project_iam_member" "cicd_artifact_registry" {
  project = var.project_id
  role    = "roles/artifactregistry.writer"
  member  = "serviceAccount:${google_service_account.cicd.email}"
}

# Deploy new revisions to Cloud Run
resource "google_project_iam_member" "cicd_run_developer" {
  project = var.project_id
  role    = "roles/run.developer"
  member  = "serviceAccount:${google_service_account.cicd.email}"
}

# Allow CI/CD to act as the API service account (for Cloud Run --service-account)
resource "google_service_account_iam_member" "cicd_act_as_api" {
  service_account_id = google_service_account.api.name
  role               = "roles/iam.serviceAccountUser"
  member             = "serviceAccount:${google_service_account.cicd.email}"
}

# Upload migration bundles to GCS
resource "google_storage_bucket_iam_member" "cicd_migrations_rw" {
  bucket = google_storage_bucket.migrations.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.cicd.email}"
}

# ── Allow unauthenticated requests to the API ─────────────────────────────────
# Public API — authentication is handled at the application layer (Firebase JWTs).

resource "google_cloud_run_v2_service_iam_member" "api_public" {
  project  = var.project_id
  location = var.region
  name     = google_cloud_run_v2_service.api.name
  role     = "roles/run.invoker"
  member   = "allUsers"
}
