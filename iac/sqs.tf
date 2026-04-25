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

# SQS Queue for asynchronous voice command processing
resource "aws_sqs_queue" "voice_commands" {
  name                       = "agrolink-voice-commands"
  visibility_timeout_seconds = 1800  # 6x Lambda timeout to reduce duplicate redelivery
  message_retention_seconds  = 86400 # 1 day — matches S3 temp file lifetime

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.voice_commands_dlq.arn
    maxReceiveCount     = 3
  })

  tags = local.common_tags
}

resource "aws_sqs_queue" "voice_commands_dlq" {
  name = "agrolink-voice-commands-dlq"
  tags = local.common_tags
}

output "voice_commands_queue_url" {
  value = aws_sqs_queue.voice_commands.url
}

