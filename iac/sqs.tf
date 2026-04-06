# SQS Queue for asynchronous Telegram updates
resource "aws_sqs_queue" "telegram_updates" {
  name                       = "agrolink-telegram-updates"
  visibility_timeout_seconds = 1800  # 6x Lambda timeout (300s) to reduce duplicate redelivery
  message_retention_seconds  = 86400 # 1 day

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.telegram_updates_dlq.arn
    maxReceiveCount     = 3
  })

  tags = local.common_tags
}

# Dead Letter Queue for failed updates
resource "aws_sqs_queue" "telegram_updates_dlq" {
  name = "agrolink-telegram-updates-dlq"
  tags = local.common_tags
}

# Output the queue URL
output "telegram_updates_queue_url" {
  value = aws_sqs_queue.telegram_updates.url
}

# Queue for external API work items that should run outside VPC (OpenAI/Telegram).
resource "aws_sqs_queue" "external_api_requests" {
  name                       = "agrolink-external-api-requests"
  visibility_timeout_seconds = 1800  # 6x Lambda timeout (300s) to reduce duplicate redelivery
  message_retention_seconds  = 86400

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.external_api_requests_dlq.arn
    maxReceiveCount     = 3
  })

  tags = local.common_tags
}

resource "aws_sqs_queue" "external_api_requests_dlq" {
  name = "agrolink-external-api-requests-dlq"
  tags = local.common_tags
}

# Queue where the public worker posts results back for VPC-side processing.
resource "aws_sqs_queue" "external_api_results" {
  name                       = "agrolink-external-api-results"
  visibility_timeout_seconds = 1800  # 6x Lambda timeout (300s) to reduce duplicate redelivery
  message_retention_seconds  = 86400

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.external_api_results_dlq.arn
    maxReceiveCount     = 3
  })

  tags = local.common_tags
}

resource "aws_sqs_queue" "external_api_results_dlq" {
  name = "agrolink-external-api-results-dlq"
  tags = local.common_tags
}

output "external_api_requests_queue_url" {
  value = aws_sqs_queue.external_api_requests.url
}

output "external_api_results_queue_url" {
  value = aws_sqs_queue.external_api_results.url
}
