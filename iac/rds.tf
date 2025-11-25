resource "aws_rds_cluster" "serverless_db" {
  cluster_identifier      = var.db_identifier
  engine                  = "aurora-postgresql"
  engine_version          = "16.8"
  engine_mode             = "provisioned"
  master_username         = var.db_master_username
  master_password         = random_password.agro_link_db_password.result
  database_name           = var.db_name
  kms_key_id              = aws_kms_key.rds_encryption_key_id.arn
  storage_encrypted       = true
  backup_retention_period = 7
  preferred_backup_window = "07:00-09:00"
  enable_http_endpoint    = true
  deletion_protection     = true
  serverlessv2_scaling_configuration {
    max_capacity = 1.0 # ACU (Aurora Capacity Units)
    min_capacity = 0.0 # Setting to 0 enables auto-pause - database will pause when idle
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

resource "aws_db_subnet_group" "main" {
  name       = "main-subnet-group"
  subnet_ids = aws_subnet.private[*].id

  tags = {
    Name = "main-subnet-group"
  }
}