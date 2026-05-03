variable "project_id" {
  description = "GCP project ID (same project as Firebase)"
  type        = string
}

variable "region" {
  description = "GCP region for all resources"
  type        = string
  default     = "us-central1"
}

variable "domain_name" {
  description = "Custom domain for the API"
  type        = string
  default     = "mifincadigital.com"
}

variable "alert_email" {
  description = "Email address to receive billing and system alerts"
  type        = string
  default     = "jaimealv994@gmail.com"
}

variable "db_name" {
  description = "PostgreSQL database name"
  type        = string
  default     = "agrolinkdb"
}

variable "db_user" {
  description = "PostgreSQL application user"
  type        = string
  default     = "agrolink_app"
}

variable "api_image" {
  description = "Full Docker image URI for the API (e.g. region-docker.pkg.dev/project/repo/api:sha)"
  type        = string
  default     = "us-docker.pkg.dev/cloudrun/container/hello:latest"
}

variable "telegram_bot_token" {
  description = "Telegram bot token"
  type        = string
  sensitive   = true
}

variable "telegram_webhook_secret_token" {
  description = "Secret token expected in Telegram webhook header"
  type        = string
  sensitive   = true
}

variable "openai_api_key" {
  description = "OpenAI API key"
  type        = string
  sensitive   = true
}

variable "github_repo" {
  description = "GitHub repository in owner/name format"
  type        = string
  default     = "jaimealvarezl/agrolink-api"
}

variable "firebase_project_id" {
  description = "Firebase project ID (usually same as GCP project_id)"
  type        = string
}

variable "enable_lambda_vpc" {
  description = "Unused — declared to suppress TFC org-level variable warning"
  type        = bool
  default     = false
}
