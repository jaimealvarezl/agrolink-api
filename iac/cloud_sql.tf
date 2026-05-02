resource "random_password" "db_password" {
  length  = 32
  special = false
}

resource "google_sql_database_instance" "postgres" {
  name             = "agrolink-pg"
  database_version = "POSTGRES_16"
  region           = var.region

  settings {
    tier              = "db-f1-micro"
    availability_type = "ZONAL"
    disk_size         = 20
    disk_type         = "PD_SSD"
    disk_autoresize   = true

    backup_configuration {
      enabled                        = true
      start_time                     = "03:00"
      point_in_time_recovery_enabled = true
      backup_retention_settings {
        retained_backups = 7
      }
    }

    ip_configuration {
      ipv4_enabled = false
      # Cloud Run connects via the Cloud SQL connector — no public IP needed.
      psc_config {
        psc_enabled               = false
        allowed_consumer_projects = []
      }
    }

    insights_config {
      query_insights_enabled = true
    }

    database_flags {
      name  = "max_connections"
      value = "100"
    }
  }

  deletion_protection = true

  user_labels = local.common_labels
}

resource "google_sql_database" "agrolink" {
  name     = var.db_name
  instance = google_sql_database_instance.postgres.name
}

resource "google_sql_user" "app" {
  name     = var.db_user
  instance = google_sql_database_instance.postgres.name
  password = random_password.db_password.result
}

# Store the password in Secret Manager
resource "google_secret_manager_secret" "db_password" {
  secret_id = "agrolink-db-password"
  labels    = local.common_labels

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "db_password" {
  secret      = google_secret_manager_secret.db_password.id
  secret_data = random_password.db_password.result
}

# Convenience output: the full connection string (password fetched from Secret Manager at runtime)
locals {
  db_connection_string = "Host=/cloudsql/${google_sql_database_instance.postgres.connection_name};Database=${var.db_name};Username=${var.db_user};Password=${random_password.db_password.result}"
}
