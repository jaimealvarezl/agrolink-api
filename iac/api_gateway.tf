resource "aws_api_gateway_rest_api" "agro_link_api" {
  name = "AgroLinkAPI"

  tags = {
    Service = "AgroLink"
  }
}

resource "aws_api_gateway_resource" "agro_link_proxy" {
  rest_api_id = aws_api_gateway_rest_api.agro_link_api.id
  parent_id   = aws_api_gateway_rest_api.agro_link_api.root_resource_id
  path_part   = "{proxy+}"
}

resource "aws_api_gateway_method" "proxy_method" {
  rest_api_id   = aws_api_gateway_rest_api.agro_link_api.id
  resource_id   = aws_api_gateway_resource.agro_link_proxy.id
  http_method   = "ANY"
  authorization = "NONE"
  request_parameters = {
    "method.request.path.proxy" = true
  }
}

resource "aws_api_gateway_method" "root_method" {
  rest_api_id   = aws_api_gateway_rest_api.agro_link_api.id
  resource_id   = aws_api_gateway_rest_api.agro_link_api.root_resource_id
  http_method   = "ANY"
  authorization = "NONE"
}

resource "aws_api_gateway_integration" "proxy_integration" {
  http_method             = aws_api_gateway_method.proxy_method.http_method
  resource_id             = aws_api_gateway_resource.agro_link_proxy.id
  rest_api_id             = aws_api_gateway_rest_api.agro_link_api.id
  type                    = "AWS_PROXY"
  cache_namespace         = "urvzgl"
  cache_key_parameters    = []
  integration_http_method = "POST"
  uri                     = aws_lambda_function.agro_link.invoke_arn
}

resource "aws_api_gateway_integration" "root_integration" {
  rest_api_id             = aws_api_gateway_rest_api.agro_link_api.id
  resource_id             = aws_api_gateway_rest_api.agro_link_api.root_resource_id
  http_method             = aws_api_gateway_method.root_method.http_method
  type                    = "AWS_PROXY"
  cache_key_parameters    = []
  cache_namespace         = "tj6b3q7qpj"
  integration_http_method = "POST"
  uri                     = aws_lambda_function.agro_link.invoke_arn
}

resource "aws_api_gateway_deployment" "api_gateway_deployment" {
  depends_on = [
    aws_api_gateway_integration.proxy_integration,
    aws_api_gateway_integration.root_integration,
  ]

  rest_api_id = aws_api_gateway_rest_api.agro_link_api.id
}

resource "aws_api_gateway_stage" "prod" {
  rest_api_id          = aws_api_gateway_rest_api.agro_link_api.id
  deployment_id        = aws_api_gateway_deployment.api_gateway_deployment.id
  stage_name           = "Prod"
  xray_tracing_enabled = true
  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.apigw_access_logs.arn
    format = jsonencode({
      requestId         = "$context.requestId",
      extendedRequestId = "$context.extendedRequestId",
      ip                = "$context.identity.sourceIp",
      caller            = "$context.identity.caller",
      user              = "$context.identity.user",
      requestTime       = "$context.requestTime",
      httpMethod        = "$context.httpMethod",
      resourcePath      = "$context.resourcePath",
      status            = "$context.status",
      protocol          = "$context.protocol",
      responseLength    = "$context.responseLength",
      integrationError  = "$context.integration.error"
    })
  }
  tags = {
    Service = "AgroLink"
  }
}

resource "aws_api_gateway_stage" "stage" {
  rest_api_id          = aws_api_gateway_rest_api.agro_link_api.id
  stage_name           = "Stage"
  deployment_id        = aws_api_gateway_deployment.api_gateway_deployment.id
  xray_tracing_enabled = true
  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.apigw_access_logs.arn
    format = jsonencode({
      requestId    = "$context.requestId",
      httpMethod   = "$context.httpMethod",
      resourcePath = "$context.resourcePath",
      status       = "$context.status"
    })
  }
  tags = {
    Service = "AgroLink"
  }
}

resource "aws_cloudwatch_log_group" "apigw_access_logs" {
  name              = "/aws/apigw/${aws_api_gateway_rest_api.agro_link_api.name}/access"
  retention_in_days = 30
  tags              = local.common_tags
}