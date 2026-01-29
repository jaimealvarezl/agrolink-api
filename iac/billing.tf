# SNS Topic for Alerts
resource "aws_sns_topic" "alerts" {
  name = "agrolink-system-alerts"
  tags = local.common_tags
}

resource "aws_sns_topic_subscription" "alerts_email" {
  topic_arn = aws_sns_topic.alerts.arn
  protocol  = "email"
  endpoint  = var.alert_email
}

# Billing Alarm
# Note: Billing metrics are only available in us-east-1
resource "aws_cloudwatch_metric_alarm" "billing_alarm_50" {
  alarm_name          = "agrolink-billing-alarm-50"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "EstimatedCharges"
  namespace           = "AWS/Billing"
  period              = "21600" # 6 hours
  statistic           = "Maximum"
  threshold           = "50"
  alarm_description   = "Estimated charges exceed $50"
  alarm_actions       = [aws_sns_topic.alerts.arn]

  dimensions = {
    Currency = "USD"
  }
}

resource "aws_cloudwatch_metric_alarm" "billing_alarm_20" {
  alarm_name          = "agrolink-billing-alarm-20"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "EstimatedCharges"
  namespace           = "AWS/Billing"
  period              = "21600" # 6 hours
  statistic           = "Maximum"
  threshold           = "20"
  alarm_description   = "Estimated charges exceed $20"
  alarm_actions       = [aws_sns_topic.alerts.arn]

  dimensions = {
    Currency = "USD"
  }
}
