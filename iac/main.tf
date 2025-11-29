terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 5.82.1"
    }
    archive = {
      source  = "hashicorp/archive"
      version = ">= 2.6"
    }
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

locals {
  common_tags = {
    Service     = "AgroLink"
    Environment = "Production"
  }
}

output "rds_endpoint" {
  description = "The RDS Cluster Endpoint"
  value       = aws_rds_cluster.serverless_db.endpoint
}

// Static website hosting disabled on SPA bucket; served via CloudFront
output "spa_distribution_domain_name" {
  description = "CloudFront domain name for the SPA"
  value       = aws_cloudfront_distribution.s3_distribution.domain_name
}

output "lambda_deployer_access_key_id" {
  value = aws_iam_access_key.lambda_code_deployer_access_key.id
}

output "lambda_deployer_secret_access_key" {
  value     = aws_iam_access_key.lambda_code_deployer_access_key.secret
  sensitive = true
}

output "spa_deployer_access_key_id" {
  value       = aws_iam_access_key.spa_deployer_access_key.id
  description = "Access Key ID for the SPA Deployer"
}

output "spa_deployer_secret_access_key" {
  value       = aws_iam_access_key.spa_deployer_access_key.secret
  description = "Secret Access Key for the SPA Deployer"
  sensitive   = true
}

output "api_gateway_url" {
  description = "API Gateway base URL"
  value       = aws_api_gateway_stage.prod.invoke_url
}

output "migration_endpoint_url" {
  description = "Migration endpoint URL (add /api/migration/run to your app)"
  value       = "${aws_api_gateway_stage.prod.invoke_url}/api/migration/run"
}

output "migration_lambda_function_name" {
  description = "Migration Lambda function name"
  value       = aws_lambda_function.migration.function_name
}

output "migration_lambda_function_arn" {
  description = "Migration Lambda function ARN"
  value       = aws_lambda_function.migration.arn
}

output "migration_lambda_vpc_config" {
  description = "Migration Lambda VPC configuration (should show subnet_ids and security_group_ids)"
  value       = aws_lambda_function.migration.vpc_config
}
