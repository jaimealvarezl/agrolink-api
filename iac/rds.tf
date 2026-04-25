resource "aws_rds_cluster" "serverless_db" {
  cluster_identifier                  = var.db_identifier
  engine                              = "aurora-postgresql"
  engine_version                      = "16.11"
  engine_mode                         = "provisioned"
  master_username                     = var.db_master_username
  master_password                     = random_password.agro_link_db_password.result
  database_name                       = var.db_name
  kms_key_id                          = aws_kms_key.rds_encryption_key_id.arn
  storage_encrypted                   = true
  backup_retention_period             = 7
  preferred_backup_window             = "07:00-09:00"
  enable_http_endpoint                = true
  iam_database_authentication_enabled = true
  deletion_protection                 = true
  serverlessv2_scaling_configuration {
    max_capacity = 2.0 # ACU (Aurora Capacity Units)
    min_capacity = 0.0
  }
  vpc_security_group_ids = [aws_security_group.rds_sg.id]
  db_subnet_group_name   = aws_db_subnet_group.main.name
  skip_final_snapshot    = true
  tags = {
    Service     = "AgroLink",
    Environment = "Production"
  }
}

resource "aws_rds_cluster_instance" "serverless_db_instance" {
  cluster_identifier = aws_rds_cluster.serverless_db.id
  instance_class     = "db.serverless"
  engine             = aws_rds_cluster.serverless_db.engine
  engine_version     = aws_rds_cluster.serverless_db.engine_version
}

# Creates the IAM-authenticated DB user on first apply (and if the cluster is ever recreated).
# Requires the CI/CD role to have rds-data:ExecuteStatement and secretsmanager:GetSecretValue.
resource "null_resource" "create_iam_db_user" {
  triggers = {
    cluster_resource_id = aws_rds_cluster.serverless_db.cluster_resource_id
  }

  provisioner "local-exec" {
    command = <<-EOT
      set -e
      RES="${aws_rds_cluster.serverless_db.arn}"
      SEC="${aws_secretsmanager_secret.agro_link_db_connection.arn}"
      DB="${var.db_name}"
      REG="${var.region}"

      sql() {
        aws rds-data execute-statement \
          --region "$REG" --resource-arn "$RES" --secret-arn "$SEC" --database "$DB" \
          --sql "$1" > /dev/null
      }

      sql "DO \$\$ BEGIN CREATE USER agrolink_app WITH LOGIN; EXCEPTION WHEN duplicate_object THEN NULL; END \$\$;"
      sql "GRANT rds_iam TO agrolink_app;"
      sql "GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO agrolink_app;"
      sql "GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO agrolink_app;"
      sql "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO agrolink_app;"
      sql "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO agrolink_app;"
    EOT
  }

  depends_on = [
    aws_rds_cluster.serverless_db,
    aws_rds_cluster_instance.serverless_db_instance,
  ]
}

resource "aws_db_subnet_group" "main" {
  name       = "main-subnet-group"
  subnet_ids = aws_subnet.private[*].id

  tags = {
    Name = "main-subnet-group"
  }
}
