terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 6.30.0"
    }
    archive = {
      source  = "hashicorp/archive"
      version = ">= 2.7.0"
    }

    google = {
      source  = "hashicorp/google"
      version = ">= 6.0.0"
    }
    random = {
      source  = "hashicorp/random"
      version = ">= 3.6.0"
    }
  }

  backend "gcs" {
    bucket = "agrolink-terraform-state"
    prefix = "terraform/state"
  }
}

provider "aws" {
  region = var.region

  default_tags {
    tags = {
      Owner = "Ops"
    }
  }
}


provider "google" {
  project = var.project_id
  region  = var.region
}

locals {
  common_labels = {
    service     = "agrolink"
    environment = "production"
    managed-by  = "terraform"
  }
}

# ── Outputs ──────────────────────────────────────────────────────────────────

output "api_url" {
  description = "Cloud Run API service URL"
  value       = google_cloud_run_v2_service.api.uri
}

output "artifact_registry_repository" {
  description = "Artifact Registry repository for Docker images"
  value       = "${var.region}-docker.pkg.dev/${var.project_id}/${google_artifact_registry_repository.images.repository_id}"
}

output "db_instance_connection_name" {
  description = "Cloud SQL connection name for Cloud Run"
  value       = google_sql_database_instance.postgres.connection_name
}

output "files_bucket_name" {
  description = "GCS bucket for file storage"
  value       = google_storage_bucket.files.name
}

output "spa_bucket_name" {
  description = "GCS bucket for SPA hosting"
  value       = google_storage_bucket.spa.name
}

output "api_service_account_email" {
  description = "Service account email for the API"
  value       = google_service_account.api.email
}

output "cicd_service_account_email" {
  description = "Service account email for CI/CD"
  value       = google_service_account.cicd.email
}
