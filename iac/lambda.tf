resource "aws_lambda_function" "agro_link" {
  function_name = "AgroLinkAPI-AspNetCoreFunction"
  handler       = var.use_placeholder ? var.placeholder_handler : "AgroLink.Api"
  runtime       = var.use_placeholder ? var.placeholder_runtime : "dotnet10"
  role          = aws_iam_role.lambda_function_role.arn
  memory_size   = 512
  timeout       = 300
  architectures = ["arm64"]

  s3_bucket  = aws_s3_bucket.lambda_code_bucket.bucket
  s3_key     = var.use_placeholder ? var.lambda_placeholder_key : var.lambda_package_key
  depends_on = [aws_s3_bucket.lambda_code_bucket, aws_s3_object.lambda_placeholder_object]

  vpc_config {
    subnet_ids         = [aws_subnet.private[0].id] # PIN TO AZ 0
    security_group_ids = [aws_security_group.lambda_sg.id]
  }

  environment {
    variables = {
      # Secret ARNs - read from Secrets Manager at runtime
      AgroLink__DbSecretArn        = aws_secretsmanager_secret.agro_link_db_connection.arn
      AgroLink__JwtSecretArn       = aws_secretsmanager_secret.jwt_secret_key.arn
      AgroLink__S3BucketName       = aws_s3_bucket.file_storage.bucket
      Telegram__BotToken           = var.telegram_bot_token
      Telegram__WebhookSecretToken = var.telegram_webhook_secret_token
      Telegram__SqsQueueUrl        = aws_sqs_queue.telegram_updates.url
      OpenAI__ApiKey               = var.openai_api_key
    }
  }

  tags = merge(local.common_tags, {
    AWSServerlessAppNETCore = true,
    "lambda:createdBy"      = "SAM"
  })
}

resource "aws_lambda_function" "telegram_sqs_consumer" {
  function_name = "AgroLink-TelegramSqsConsumer"
  handler       = var.use_placeholder ? var.placeholder_handler : "AgroLink.Workers::AgroLink.Workers.SqsFunction::FunctionHandler"
  runtime       = var.use_placeholder ? var.placeholder_runtime : "dotnet10"
  role          = aws_iam_role.lambda_function_role.arn
  memory_size   = 512
  timeout       = 300
  architectures = ["arm64"]

  s3_bucket  = aws_s3_bucket.lambda_code_bucket.bucket
  s3_key     = var.use_placeholder ? var.lambda_placeholder_key : var.lambda_package_key
  depends_on = [aws_s3_bucket.lambda_code_bucket, aws_s3_object.lambda_placeholder_object]

  vpc_config {
    subnet_ids         = [aws_subnet.private[0].id] # PIN TO AZ 0
    security_group_ids = [aws_security_group.lambda_sg.id]
  }

  environment {
    variables = {
      AgroLink__DbSecretArn               = aws_secretsmanager_secret.agro_link_db_connection.arn
      AgroLink__JwtSecretArn              = aws_secretsmanager_secret.jwt_secret_key.arn
      AgroLink__S3BucketName              = aws_s3_bucket.file_storage.bucket
      Telegram__BotToken                  = var.telegram_bot_token
      Telegram__WebhookSecretToken        = var.telegram_webhook_secret_token
      Telegram__SqsQueueUrl               = aws_sqs_queue.telegram_updates.url
      ExternalWorkers__WorkerFunctionName = aws_lambda_function.external_api_worker.function_name
      OpenAI__ApiKey                      = var.openai_api_key
    }
  }

  tags = merge(local.common_tags, {
    AWSServerlessAppNETCore = true,
    "lambda:createdBy"      = "SAM"
  })
}

resource "aws_lambda_function" "external_api_worker" {
  function_name = "AgroLink-ExternalApiWorker"
  handler       = var.use_placeholder ? var.placeholder_handler : "AgroLink.Workers::AgroLink.Workers.ExternalApiWorkerFunction::FunctionHandler"
  runtime       = var.use_placeholder ? var.placeholder_runtime : "dotnet10"
  role          = aws_iam_role.lambda_function_role.arn
  memory_size   = 512
  timeout       = 300
  architectures = ["arm64"]

  s3_bucket  = aws_s3_bucket.lambda_code_bucket.bucket
  s3_key     = var.use_placeholder ? var.lambda_placeholder_key : var.lambda_package_key
  depends_on = [aws_s3_bucket.lambda_code_bucket, aws_s3_object.lambda_placeholder_object]

  environment {
    variables = {
      Telegram__BotToken = var.telegram_bot_token
      OpenAI__ApiKey     = var.openai_api_key
    }
  }

  tags = merge(local.common_tags, {
    AWSServerlessAppNETCore = true,
    "lambda:createdBy"      = "SAM"
  })
}

resource "aws_cloudwatch_log_group" "lambda_log_group" {
  name              = "/aws/lambda/${aws_lambda_function.agro_link.function_name}"
  retention_in_days = 30
  tags = {
    Service     = "AgroLink",
    Environment = "Production"
  }
}

resource "aws_cloudwatch_log_group" "telegram_sqs_consumer_log_group" {
  name              = "/aws/lambda/${aws_lambda_function.telegram_sqs_consumer.function_name}"
  retention_in_days = 30
  tags = {
    Service     = "AgroLink",
    Environment = "Production"
  }
}

resource "aws_cloudwatch_log_group" "external_api_worker_log_group" {
  name              = "/aws/lambda/${aws_lambda_function.external_api_worker.function_name}"
  retention_in_days = 30
  tags = {
    Service     = "AgroLink",
    Environment = "Production"
  }
}

resource "aws_lambda_permission" "agro_link_lambda_permission" {
  statement_id  = "AgroLinkAPI-AspNetCoreFunctionProxyResourcePermissionProd"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.agro_link.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_api_gateway_rest_api.agro_link_api.execution_arn}/*/*/*"
}

resource "aws_lambda_permission" "agro_link_root_lambda_permission" {
  statement_id  = "AgroLinkAPI-AspNetCoreFunctionRootResourcePermissionProd"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.agro_link.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_api_gateway_rest_api.agro_link_api.execution_arn}/*/*/"
}

resource "aws_lambda_function" "voice_command_cleanup" {
  function_name = "AgroLink-VoiceCommandCleanup"
  handler       = var.use_placeholder ? var.placeholder_handler : "AgroLink.Workers::AgroLink.Workers.CleanupFunction::FunctionHandler"
  runtime       = var.use_placeholder ? var.placeholder_runtime : "dotnet10"
  role          = aws_iam_role.lambda_function_role.arn
  memory_size   = 256
  timeout       = 60
  architectures = ["arm64"]

  s3_bucket  = aws_s3_bucket.lambda_code_bucket.bucket
  s3_key     = var.use_placeholder ? var.lambda_placeholder_key : var.lambda_package_key
  depends_on = [aws_s3_bucket.lambda_code_bucket, aws_s3_object.lambda_placeholder_object]

  vpc_config {
    subnet_ids         = [aws_subnet.private[0].id]
    security_group_ids = [aws_security_group.lambda_sg.id]
  }

  environment {
    variables = {
      AgroLink__DbSecretArn  = aws_secretsmanager_secret.agro_link_db_connection.arn
      AgroLink__JwtSecretArn = aws_secretsmanager_secret.jwt_secret_key.arn
    }
  }

  tags = merge(local.common_tags, {
    AWSServerlessAppNETCore = true
  })
}

resource "aws_cloudwatch_log_group" "voice_command_cleanup_log_group" {
  name              = "/aws/lambda/${aws_lambda_function.voice_command_cleanup.function_name}"
  retention_in_days = 30
  tags              = local.common_tags
}

resource "aws_cloudwatch_event_rule" "voice_command_cleanup_schedule" {
  name                = "agrolink-voice-command-cleanup-daily"
  description         = "Triggers VoiceCommandJob cleanup Lambda every Sunday at midnight UTC"
  schedule_expression = "cron(0 0 ? * SUN *)"
  tags                = local.common_tags
}

resource "aws_cloudwatch_event_target" "voice_command_cleanup_target" {
  rule      = aws_cloudwatch_event_rule.voice_command_cleanup_schedule.name
  target_id = "VoiceCommandCleanupLambda"
  arn       = aws_lambda_function.voice_command_cleanup.arn
}

resource "aws_lambda_permission" "allow_eventbridge_invoke_cleanup" {
  statement_id  = "AllowEventBridgeInvokeCleanup"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.voice_command_cleanup.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.voice_command_cleanup_schedule.arn
}

# SQS Trigger for the Lambda function
resource "aws_lambda_event_source_mapping" "sqs_trigger" {
  event_source_arn = aws_sqs_queue.telegram_updates.arn
  function_name    = aws_lambda_function.telegram_sqs_consumer.arn
  batch_size       = 1 # Process one Telegram update at a time
  enabled          = true
}

