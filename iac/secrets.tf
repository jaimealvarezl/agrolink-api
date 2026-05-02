# ── Application Secrets ───────────────────────────────────────────────────────
# Secrets are created here; values are managed outside Terraform or via CI/CD.

resource "google_secret_manager_secret" "telegram_bot_token" {
  secret_id = "agrolink-telegram-bot-token"
  labels    = local.common_labels
  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "telegram_bot_token" {
  secret      = google_secret_manager_secret.telegram_bot_token.id
  secret_data = var.telegram_bot_token
}

resource "google_secret_manager_secret" "telegram_webhook_secret" {
  secret_id = "agrolink-telegram-webhook-secret"
  labels    = local.common_labels
  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "telegram_webhook_secret" {
  secret      = google_secret_manager_secret.telegram_webhook_secret.id
  secret_data = var.telegram_webhook_secret_token
}

resource "google_secret_manager_secret" "openai_api_key" {
  secret_id = "agrolink-openai-api-key"
  labels    = local.common_labels
  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "openai_api_key" {
  secret      = google_secret_manager_secret.openai_api_key.id
  secret_data = var.openai_api_key
}

resource "google_secret_manager_secret" "scheduler_secret" {
  secret_id = "agrolink-scheduler-secret"
  labels    = local.common_labels
  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "scheduler_secret" {
  secret      = google_secret_manager_secret.scheduler_secret.id
  secret_data = var.scheduler_secret
}
