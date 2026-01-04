variable "region" {
  description = "AWS region to deploy resources"
  type        = string
  default     = "us-east-1"
}

variable "enable_lambda_vpc" {
  description = "Whether to attach Lambda to VPC (set false to ease destroy)"
  type        = bool
  default     = true
}

variable "db_master_username" {
  description = "Master username for the database"
  type        = string
  default     = "agrolinkadmin"
}

variable "db_identifier" {
  description = "RDS Cluster identifier"
  type        = string
  default     = "agrolink-pg-cluster"
}

variable "db_name" {
  description = "Name of the database"
  type        = string
  default     = "agrolinkdb"
}

variable "code_bucket_name" {
  description = "Name of the S3 bucket to store Lambda code"
  type        = string
  default     = "agrolink-lambda-code-bucket"
}

variable "lambda_package_key" {
  description = "S3 key for the Lambda deployment package zip (real app)"
  type        = string
  default     = "AgroLink.API/latest.zip"
}

variable "use_placeholder" {
  description = "If true, deploy a minimal placeholder Lambda until real code is ready"
  type        = bool
  default     = false
}

variable "lambda_placeholder_key" {
  description = "S3 key to upload the generated placeholder zip to"
  type        = string
  default     = "placeholder/placeholder.zip"
}

variable "placeholder_runtime" {
  description = "Runtime to use for the placeholder function"
  type        = string
  default     = "python3.12"
}

variable "placeholder_handler" {
  description = "Handler to use for the placeholder function"
  type        = string
  default     = "index.handler"
}

variable "spa_bucket_name" {
  description = "Name of the S3 bucket to host the SPA"
  type        = string
  default     = "agrolink-spa-bucket"
}

variable "storage_bucket_name" {
  description = "Name of the S3 bucket for storing files and pictures"
  type        = string
  default     = "agrolink-storage-bucket"
}

variable "domain_name" {
  default = "mifincadigital.com"
}

variable "hosted_zone_id" {
  default = "Z05293003VAY8JBOBCB8P"
}
