resource "aws_iam_policy" "spa_bucket_deploy_policy" {
  name        = "SPA_Bucket_Deploy_Policy"
  description = "Policy granting permissions to upload React SPA code to spa_bucket"

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Sid    = "S3Access",
        Effect = "Allow",
        Action = [
          "s3:ListBucket",
          "s3:PutObject",
          "s3:PutObjectAcl",
          "s3:GetObject",
          "s3:DeleteObject",
        ],
        Resource = [
          aws_s3_bucket.spa_bucket.arn,
          "${aws_s3_bucket.spa_bucket.arn}/*"
        ]
      }
    ]
  })
}

resource "aws_iam_user" "spa_deployer" {
  name = "SPA_Deployer"

  tags = {
    Name        = "SPA Deployer User"
    Environment = "Production"
  }
}

resource "aws_iam_user_policy_attachment" "spa_policy_attach" {
  user       = aws_iam_user.spa_deployer.name
  policy_arn = aws_iam_policy.spa_bucket_deploy_policy.arn
}

resource "aws_iam_access_key" "spa_deployer_access_key" {
  user = aws_iam_user.spa_deployer.name
}



resource "aws_iam_policy" "lambda_code_deploy_policy" {
  name        = "Lambda_Code_Deploy_Policy"
  description = "Policy granting permissions to upload Lambda code to lambda_code_bucket and manage Lambda functions"

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Sid    = "S3Access",
        Effect = "Allow",
        Action = [
          "s3:ListBucket",
          "s3:GetBucketLocation",
          "s3:ListBucketMultipartUploads",
          "s3:ListBucketVersions",
          "s3:PutObject",
          "s3:PutObjectAcl",
          "s3:GetObject",
          "s3:GetObjectVersion",
          "s3:DeleteObject",
          "s3:AbortMultipartUpload"
        ],
        Resource = [
          aws_s3_bucket.lambda_code_bucket.arn,
          "${aws_s3_bucket.lambda_code_bucket.arn}/*",
          # Allow access to migrations bucket (if different from lambda bucket)
          "arn:aws:s3:::agrolink-migrations",
          "arn:aws:s3:::agrolink-migrations/*"
        ]
      },
      {
        Sid    = "LambdaReadAndCreate",
        Effect = "Allow",
        Action = [
          "lambda:GetFunction",
          "lambda:GetFunctionConfiguration",
          "lambda:ListFunctions",
          "lambda:CreateFunction",
          "lambda:ListVersionsByFunction"
        ],
        Resource = "*"
      },
      {
        Sid    = "LambdaAccess",
        Effect = "Allow",
        Action = [
          "lambda:UpdateFunctionCode",
          "lambda:UpdateFunctionConfiguration",
          "lambda:PublishVersion",
          "lambda:CreateAlias",
          "lambda:UpdateAlias",
          "lambda:ListAliases",
          "lambda:GetAlias",
          "lambda:GetFunction",
          "lambda:GetFunctionConfiguration"
        ],
        Resource = [
          aws_lambda_function.agro_link.arn,
          "${aws_lambda_function.agro_link.arn}:*"
        ]
      },
      {
        Sid    = "PassExecutionRole",
        Effect = "Allow",
        Action = [
          "iam:PassRole"
        ],
        Resource = aws_iam_role.lambda_function_role.arn
      },
      {
        Sid    = "CloudWatchLogs",
        Effect = "Allow",
        Action = [
          "logs:DescribeLogGroups",
          "logs:DescribeLogStreams",
          "logs:GetLogEvents"
        ],
        Resource = "arn:aws:logs:${var.region}:*:log-group:/aws/lambda/${aws_lambda_function.agro_link.function_name}:*"
      },
      {
        Sid    = "EC2ReadAccess",
        Effect = "Allow",
        Action = [
          "ec2:DescribeSecurityGroups",
          "ec2:DescribeSubnets",
          "ec2:DescribeVpcs"
        ],
        Resource = "*"
      },
      {
        Sid    = "STSGetCallerIdentity",
        Effect = "Allow",
        Action = [
          "sts:GetCallerIdentity"
        ],
        Resource = "*"
      },
      {
        Sid    = "MigrationLambdaInvoke",
        Effect = "Allow",
        Action = [
          "lambda:InvokeFunction"
        ],
        Resource = aws_lambda_function.migration.arn
      }
    ]
  })
}

resource "aws_iam_user" "lambda_code_deployer" {
  name = "Lambda_Code_Deployer"

  tags = {
    Name        = "Lambda Code Deployer User"
    Environment = "Production"
  }
}

resource "aws_iam_user_policy_attachment" "lambda_code_policy_attach" {
  user       = aws_iam_user.lambda_code_deployer.name
  policy_arn = aws_iam_policy.lambda_code_deploy_policy.arn
}

resource "aws_iam_access_key" "lambda_code_deployer_access_key" {
  user = aws_iam_user.lambda_code_deployer.name
}

resource "aws_iam_role" "lambda_function_role" {
  name = "AgroLinkAPI-AspNetCoreFunctionRole"

  assume_role_policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Action = "sts:AssumeRole",
        Effect = "Allow",
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      },
    ]
  })

  tags = merge(local.common_tags, {
    AWSServerlessAppNETCore = "true",
    "lambda:createdBy"      = "SAM"
  })

  tags_all = {
    AWSServerlessAppNETCore = "true",
    "lambda:createdBy"      = "SAM"
  }
}

resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.lambda_function_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy_attachment" "AWSLambda_FullAccess" {
  role       = aws_iam_role.lambda_function_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSLambda_FullAccess"
}

resource "aws_iam_role_policy_attachment" "lambda_vpc_access" {
  role       = aws_iam_role.lambda_function_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

# Allow Lambda to read secrets from Secrets Manager and decrypt if needed
resource "aws_iam_role_policy" "lambda_secrets_access" {
  name = "AgroLinkLambdaSecretsAccess"
  role = aws_iam_role.lambda_function_role.id
  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect = "Allow",
        Action = ["secretsmanager:GetSecretValue"],
        Resource = [
          aws_secretsmanager_secret.agro_link_db_connection.arn,
          aws_secretsmanager_secret.jwt_secret_key.arn
        ]
      },
      {
        Effect   = "Allow",
        Action   = ["kms:Decrypt"],
        Resource = aws_kms_key.rds_encryption_key_id.arn
      }
    ]
  })
}