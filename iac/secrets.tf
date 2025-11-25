resource "random_password" "random_jwt_secret_key" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "aws_secretsmanager_secret" "jwt_secret_key" {
  name = "agrolink/jwt-secret"

  tags = {
    Scope = "AgroLink"
  }
}

resource "aws_secretsmanager_secret_version" "jwt_settings_secret_key" {
  secret_id     = aws_secretsmanager_secret.jwt_secret_key.id
  secret_string = random_password.random_jwt_secret_key.result
}

resource "random_password" "agro_link_db_password" {
  length           = 20
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?" # exclude / @ " and space
}

resource "aws_secretsmanager_secret" "agro_link_db_password" {
  name = "agrolink/db-password"

  tags = {
    Scope = "AgroLink"
  }
}

resource "aws_secretsmanager_secret_version" "agro_link_db_password_secret" {
  secret_id     = aws_secretsmanager_secret.agro_link_db_password.id
  secret_string = random_password.agro_link_db_password.result
}

resource "aws_kms_key" "rds_encryption_key_id" {
  deletion_window_in_days = 30
}

# Secret that aggregates DB connection details for application use
resource "aws_secretsmanager_secret" "agro_link_db_connection" {
  name = "agrolink/db-connection"

  tags = {
    Scope = "AgroLink"
  }
}

resource "aws_secretsmanager_secret_version" "agro_link_db_connection_value" {
  secret_id = aws_secretsmanager_secret.agro_link_db_connection.id
  secret_string = jsonencode({
    # Individual fields for programmatic access
    host     = aws_rds_cluster.serverless_db.endpoint
    port     = 5432
    database = var.db_name
    username = var.db_master_username
    password = random_password.agro_link_db_password.result
    # Ready-to-use PostgreSQL connection string for Entity Framework Core / Npgsql
    connectionString = "Host=${aws_rds_cluster.serverless_db.endpoint};Port=5432;Username=${var.db_master_username};Password=${random_password.agro_link_db_password.result};Database=${var.db_name}"
  })
}