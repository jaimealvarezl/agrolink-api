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
