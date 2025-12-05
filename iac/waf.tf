# WAF Web ACL for API Gateway protection
# resource "aws_wafv2_web_acl" "api_gateway_waf" {
#   name        = "agrolink-api-gateway-waf"
#   description = "WAF for AgroLink API Gateway"
#   scope       = "REGIONAL"

#   default_action {
#     allow {}
#   }

#   rule {
#     name     = "AWSManagedRulesCommonRuleSet"
#     priority = 1

#     override_action {
#       none {}
#     }

#     statement {
#       managed_rule_group_statement {
#         name        = "AWSManagedRulesCommonRuleSet"
#         vendor_name = "AWS"
#       }
#     }

#     visibility_config {
#       cloudwatch_metrics_enabled = true
#       metric_name                = "AWSManagedRulesCommonRuleSetMetric"
#       sampled_requests_enabled   = true
#     }
#   }

#   rule {
#     name     = "RateLimitRule"
#     priority = 2

#     action {
#       block {}
#     }

#     statement {
#       rate_based_statement {
#         limit              = 2000
#         aggregate_key_type = "IP"
#       }
#     }

#     visibility_config {
#       cloudwatch_metrics_enabled = true
#       metric_name                = "RateLimitRuleMetric"
#       sampled_requests_enabled   = true
#     }
#   }

#   visibility_config {
#     cloudwatch_metrics_enabled = true
#     metric_name                = "AgroLinkAPIGatewayWAFMetric"
#     sampled_requests_enabled   = true
#   }
# }

# Associate WAF with API Gateway
# resource "aws_wafv2_web_acl_association" "api_gateway" {
#   resource_arn = aws_api_gateway_stage.prod.arn
#   web_acl_arn  = aws_wafv2_web_acl.api_gateway_waf.arn
# }