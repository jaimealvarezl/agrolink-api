# ── Artifact Registry ────────────────────────────────────────────────────────

resource "google_artifact_registry_repository" "images" {
  repository_id = "agrolink"
  location      = var.region
  format        = "DOCKER"
  description   = "Docker images for AgroLink services"
  labels        = local.common_labels
}

# ── GCS Buckets ───────────────────────────────────────────────────────────────

resource "google_storage_bucket" "files" {
  name          = "${var.project_id}-agrolink-files"
  location      = var.region
  force_destroy = false

  uniform_bucket_level_access = true

  versioning {
    enabled = false
  }

  cors {
    origin          = ["https://${var.domain_name}"]
    method          = ["GET", "HEAD"]
    response_header = ["Content-Type"]
    max_age_seconds = 3600
  }

  lifecycle_rule {
    condition {
      age = 365
    }
    action {
      type          = "SetStorageClass"
      storage_class = "NEARLINE"
    }
  }

  labels = local.common_labels
}

resource "google_storage_bucket" "spa" {
  name          = "${var.project_id}-agrolink-spa"
  location      = var.region
  force_destroy = false

  uniform_bucket_level_access = true

  website {
    main_page_suffix = "index.html"
    not_found_page   = "index.html"
  }

  cors {
    origin          = ["https://${var.domain_name}"]
    method          = ["GET", "HEAD"]
    response_header = ["*"]
    max_age_seconds = 3600
  }

  labels = local.common_labels
}

# Make SPA bucket publicly readable
resource "google_storage_bucket_iam_member" "spa_public" {
  bucket = google_storage_bucket.spa.name
  role   = "roles/storage.objectViewer"
  member = "allUsers"
}

resource "google_storage_bucket" "migrations" {
  name          = "${var.project_id}-agrolink-migrations"
  location      = var.region
  force_destroy = false

  uniform_bucket_level_access = true

  lifecycle_rule {
    condition {
      age = 30
    }
    action {
      type = "Delete"
    }
  }

  labels = local.common_labels
}
