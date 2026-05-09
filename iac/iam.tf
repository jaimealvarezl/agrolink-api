# ── Workload Identity Federation (GitHub Actions) ────────────────────────────

resource "google_iam_workload_identity_pool" "github" {
  workload_identity_pool_id = "github-pool"
  display_name              = "GitHub Actions Pool"
  depends_on                = [google_project_service.apis]
}

resource "google_iam_workload_identity_pool_provider" "github" {
  workload_identity_pool_id          = google_iam_workload_identity_pool.github.workload_identity_pool_id
  workload_identity_pool_provider_id = "github-provider"
  display_name                       = "GitHub Provider"

  attribute_mapping = {
    "google.subject"       = "assertion.sub"
    "attribute.repository" = "assertion.repository"
    "attribute.actor"      = "assertion.actor"
  }

  attribute_condition = "assertion.repository == '${var.github_repo}'"

  oidc {
    issuer_uri = "https://token.actions.githubusercontent.com"
  }
}

# Allow GitHub Actions to impersonate the cicd service account
resource "google_service_account_iam_member" "github_wif_cicd" {
  service_account_id = google_service_account.cicd.name
  role               = "roles/iam.workloadIdentityUser"
  member             = "principalSet://iam.googleapis.com/${google_iam_workload_identity_pool.github.name}/attribute.repository/${var.github_repo}"
}

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

# Allow the API service account to sign blobs (required for GCS signed URLs on Cloud Run)
resource "google_service_account_iam_member" "api_sign_blobs" {
  service_account_id = google_service_account.api.name
  role               = "roles/iam.serviceAccountTokenCreator"
  member             = "serviceAccount:${google_service_account.api.email}"
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
