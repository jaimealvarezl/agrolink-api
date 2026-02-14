resource "aws_s3_bucket" "lambda_code_bucket" {
  bucket        = var.code_bucket_name
  force_destroy = true
  tags = merge(local.common_tags, {
    Name = "Agrolink Lambda Code Bucket"
  })
}

# Generates a tiny placeholder Lambda zip when enabled
data "archive_file" "lambda_placeholder_zip" {
  type        = "zip"
  output_path = "${path.module}/.placeholder.zip"

  source {
    content  = <<-EOT
import json

def handler(event, context):
    return {
        "statusCode": 200,
        "headers": {"Content-Type": "application/json"},
        "body": json.dumps({
            "message": "Placeholder Lambda running",
            "path": event.get("path"),
            "note": "Replace with real deployment"
        })
    }
EOT
    filename = "index.py"
  }
}

resource "aws_s3_object" "lambda_placeholder_object" {
  count  = var.use_placeholder ? 1 : 0
  bucket = aws_s3_bucket.lambda_code_bucket.id
  key    = var.lambda_placeholder_key
  source = data.archive_file.lambda_placeholder_zip.output_path
  etag   = filemd5(data.archive_file.lambda_placeholder_zip.output_path)
}

resource "aws_s3_bucket_policy" "lambda_code_bucket_policy" {
  bucket = aws_s3_bucket.lambda_code_bucket.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Sid : "AllowDeployerBucketLevel",
        Effect : "Allow",
        Principal : { AWS : aws_iam_user.lambda_code_deployer.arn },
        Action : [
          "s3:GetBucketLocation",
          "s3:ListBucket",
          "s3:ListBucketMultipartUploads"
        ],
        Resource : aws_s3_bucket.lambda_code_bucket.arn
      },
      {
        Sid : "AllowDeployerObjectLevel",
        Effect : "Allow",
        Principal : { AWS : aws_iam_user.lambda_code_deployer.arn },
        Action : [
          "s3:PutObject",
          "s3:PutObjectAcl",
          "s3:GetObject",
          "s3:GetObjectVersion",
          "s3:DeleteObject",
          "s3:AbortMultipartUpload"
        ],
        Resource : "${aws_s3_bucket.lambda_code_bucket.arn}/*"
      }
    ]
  })
}

resource "aws_s3_bucket_versioning" "lambda_code_bucket_versioning" {
  bucket = aws_s3_bucket.lambda_code_bucket.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "lambda_code_bucket_lifecycle" {
  bucket = aws_s3_bucket.lambda_code_bucket.id

  rule {
    id     = "cleanup_old_versions"
    status = "Enabled"

    noncurrent_version_expiration {
      noncurrent_days = 30
    }
  }
}

resource "aws_s3_bucket" "spa_bucket" {
  bucket        = var.spa_bucket_name
  force_destroy = true
  tags = merge(local.common_tags, {
    Name = "Agrolink SPA Bucket"
  })
}

resource "aws_s3_bucket_public_access_block" "spa_bucket" {
  bucket = aws_s3_bucket.spa_bucket.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket" "file_storage" {
  bucket        = var.storage_bucket_name
  force_destroy = true
  tags = merge(local.common_tags, {
    Name = "Agrolink File Storage"
  })
}

resource "aws_s3_bucket_public_access_block" "file_storage" {
  bucket = aws_s3_bucket.file_storage.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_cors_configuration" "file_storage_cors" {
  bucket = aws_s3_bucket.file_storage.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "HEAD"]
    allowed_origins = ["*"]
    expose_headers  = ["ETag"]
    max_age_seconds = 3000
  }
}

resource "aws_s3_bucket_policy" "spa_bucket_policy" {
  bucket = aws_s3_bucket.spa_bucket.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Sid : "AllowCloudFrontOACRead",
        Effect : "Allow",
        Principal : {
          Service : "cloudfront.amazonaws.com"
        },
        Action : [
          "s3:GetObject"
        ],
        Resource : "${aws_s3_bucket.spa_bucket.arn}/*",
        Condition : {
          StringEquals : {
            "AWS:SourceArn" : aws_cloudfront_distribution.s3_distribution.arn
          }
        }
      }
    ]
  })
}
