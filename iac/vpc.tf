resource "aws_vpc" "main" {
  cidr_block = "10.0.0.0/16"

  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = merge(local.common_tags, { Name = "main-vpc" })
}

resource "aws_internet_gateway" "igw" {
  vpc_id = aws_vpc.main.id
  tags   = merge(local.common_tags, { Name = "main-igw" })
}

resource "aws_subnet" "private" {
  count             = 2
  vpc_id            = aws_vpc.main.id
  cidr_block        = element(["10.0.1.0/24", "10.0.2.0/24"], count.index)
  availability_zone = element(data.aws_availability_zones.available.names, count.index)
  
  # Even if subnets are "public" by route, we keep this false for safety
  map_public_ip_on_launch = false

  tags = merge(local.common_tags, { Name = "app-subnet-${count.index}" })
}

data "aws_availability_zones" "available" {
  state = "available"
}

resource "aws_security_group" "lambda_sg" {
  vpc_id = aws_vpc.main.id

  ingress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["10.0.0.0/16"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.common_tags, { Name = "lambda-sg" })
}

resource "aws_security_group" "rds_sg" {
  vpc_id = aws_vpc.main.id

  ingress {
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.lambda_sg.id, aws_security_group.migration_lambda_sg.id]
    description     = "Allow Lambda access to RDS"
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.common_tags, { Name = "rds-sg" })
}

# Security group for VPC Endpoints (S3 Gateway doesn't use this, but keeping for future use)
resource "aws_security_group" "vpce_sg" {
  vpc_id = aws_vpc.main.id

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.main.cidr_block]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.common_tags, { Name = "vpce-sg" })
}

# Manage the default route table to provide internet access
resource "aws_default_route_table" "main" {
  default_route_table_id = aws_vpc.main.main_route_table_id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.igw.id
  }

  tags = merge(local.common_tags, { Name = "main-default-rt" })
}

# Gateway endpoint for S3 (FREE)
resource "aws_vpc_endpoint" "s3" {
  vpc_id            = aws_vpc.main.id
  service_name      = "com.amazonaws.${var.region}.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids   = [aws_default_route_table.main.id]

  tags = merge(local.common_tags, { Name = "s3-gateway-endpoint" })
}