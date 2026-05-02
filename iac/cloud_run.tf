resource "google_cloud_run_v2_service" "api" {
  name     = "agrolink-api"
  location = var.region
  labels   = local.common_labels

  ingress = "INGRESS_TRAFFIC_ALL"

  template {
    service_account = google_service_account.api.email

    scaling {
      min_instance_count = 0
      max_instance_count = 10
    }

    timeout = "300s"

    containers {
      image = var.api_image

      ports {
        container_port = 8080
      }

      resources {
        limits = {
          cpu    = "1"
          memory = "512Mi"
        }
        cpu_idle = true
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080"
      }
      env {
        name  = "Firebase__ProjectId"
        value = var.firebase_project_id
      }
      env {
        name  = "GCS__BucketName"
        value = google_storage_bucket.files.name
      }

      # Database connection via Cloud SQL connector (Unix socket)
      env {
        name = "ConnectionStrings__DefaultConnection"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.db_password.secret_id
            version = "latest"
          }
        }
      }

      env {
        name = "Telegram__BotToken"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.telegram_bot_token.secret_id
            version = "latest"
          }
        }
      }

      env {
        name = "Telegram__WebhookSecretToken"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.telegram_webhook_secret.secret_id
            version = "latest"
          }
        }
      }

      env {
        name = "OpenAI__ApiKey"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.openai_api_key.secret_id
            version = "latest"
          }
        }
      }

      env {
        name = "Internal__SchedulerSecret"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.scheduler_secret.secret_id
            version = "latest"
          }
        }
      }

      volume_mounts {
        name       = "cloudsql"
        mount_path = "/cloudsql"
      }
    }

    volumes {
      name = "cloudsql"
      cloud_sql_instance {
        instances = [google_sql_database_instance.postgres.connection_name]
      }
    }
  }

  depends_on = [
    google_project_iam_member.api_cloudsql,
    google_project_iam_member.api_secret_accessor,
  ]
}

# ── Cloud Scheduler → API cleanup endpoint ────────────────────────────────────

resource "google_service_account" "scheduler" {
  account_id   = "agrolink-scheduler"
  display_name = "AgroLink Cloud Scheduler"
}

resource "google_cloud_scheduler_job" "cleanup" {
  name             = "agrolink-voice-command-cleanup"
  description      = "Delete stale voice command jobs daily"
  schedule         = "0 3 * * *"
  time_zone        = "America/Argentina/Buenos_Aires"
  attempt_deadline = "320s"

  http_target {
    http_method = "POST"
    uri         = "${google_cloud_run_v2_service.api.uri}/api/internal/cleanup"

    headers = {
      "X-Scheduler-Secret" = var.scheduler_secret
      "Content-Type"       = "application/json"
    }
  }
}
