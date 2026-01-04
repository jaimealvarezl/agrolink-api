resource "aws_cloudfront_origin_access_control" "spa_oac" {
  name                              = "spa-oac"
  description                       = "OAC for SPA S3 origin"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

resource "aws_cloudfront_distribution" "s3_distribution" {
  origin {
    domain_name = aws_s3_bucket.spa_bucket.bucket_regional_domain_name
    origin_id   = "S3-${aws_s3_bucket.spa_bucket.id}"

    origin_access_control_id = aws_cloudfront_origin_access_control.spa_oac.id
  }

  aliases = [var.domain_name, "www.${var.domain_name}"]

  enabled             = true
  default_root_object = "index.html"
  tags                = local.common_tags

  default_cache_behavior {
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "S3-${aws_s3_bucket.spa_bucket.id}"
    viewer_protocol_policy = "redirect-to-https"

    min_ttl     = 0
    default_ttl = 3600
    max_ttl     = 86400

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }
  }

  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  viewer_certificate {
    acm_certificate_arn            = aws_acm_certificate.cert.arn
    ssl_support_method             = "sni-only"
    minimum_protocol_version       = "TLSv1.2_2019"
    cloudfront_default_certificate = false
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }
}
 