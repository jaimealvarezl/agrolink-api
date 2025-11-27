# IAM role for migration Lambda
resource "aws_iam_role" "migration_lambda_role" {
  name = "AgroLink-Migration-Lambda-Role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })

  tags = merge(local.common_tags, {
    Name = "Migration Lambda Role"
  })
}

# IAM policy for migration Lambda
resource "aws_iam_role_policy" "migration_lambda_policy" {
  role = aws_iam_role.migration_lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:${var.region}:*:log-group:/aws/lambda/AgroLink-Migration-Function:*"
      },
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:GetObjectVersion"
        ]
        Resource = [
          "${aws_s3_bucket.lambda_code_bucket.arn}/*",
          "arn:aws:s3:::agrolink-migrations/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [
          aws_secretsmanager_secret.agro_link_db_connection.arn,
          aws_secretsmanager_secret.jwt_secret_key.arn
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "rds:DescribeDBClusters",
          "rds:DescribeDBClusterEndpoints"
        ]
        Resource = "*"
      }
    ]
  })
}

# Attach VPC access policy to migration Lambda role
resource "aws_iam_role_policy_attachment" "migration_lambda_vpc_access" {
  role       = aws_iam_role.migration_lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

# Security group for migration Lambda
resource "aws_security_group" "migration_lambda_sg" {
  vpc_id = aws_vpc.main.id

  # No ingress needed - Lambda initiates connections

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.common_tags, { Name = "migration-lambda-sg" })
}

# Update RDS security group to allow migration Lambda access
resource "aws_security_group_rule" "rds_allow_migration_lambda" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.migration_lambda_sg.id
  security_group_id        = aws_security_group.rds_sg.id
  description              = "Allow migration Lambda to connect to RDS for migrations"
}

# Migration Lambda function
resource "aws_lambda_function" "migration" {
  function_name = "AgroLink-Migration-Function"
  handler       = "index.handler"
  runtime       = "python3.11"
  role          = aws_iam_role.migration_lambda_role.arn
  memory_size   = 512
  timeout       = 900 # 15 minutes (max for Lambda)
  architectures = ["arm64"]

  # Use inline code for the migration handler
  filename         = data.archive_file.migration_lambda_zip.output_path
  source_code_hash = data.archive_file.migration_lambda_zip.output_base64sha256

  dynamic "vpc_config" {
    for_each = var.enable_lambda_vpc ? [1] : []
    content {
      subnet_ids         = aws_subnet.private[*].id
      security_group_ids = [aws_security_group.migration_lambda_sg.id]
    }
  }

  environment {
    variables = {
      DB_SECRET_ARN = aws_secretsmanager_secret.agro_link_db_connection.arn
    }
  }

  tags = merge(local.common_tags, {
    Name = "Migration Lambda Function"
  })
}

# Lambda function code (inline Python script)
data "archive_file" "migration_lambda_zip" {
  type        = "zip"
  output_path = "${path.module}/migration_lambda.zip"

  source {
    content  = <<-EOF
import json
import os
import subprocess
import boto3
import zipfile
import tempfile
import shutil
from urllib.parse import urlparse

def handler(event, context):
    """
    Lambda handler to run EF Core migrations.
    
    Expected event:
    {
        "s3_bucket": "bucket-name",
        "s3_key": "migrations/migrations-abc123.zip"
    }
    """
    try:
        # Get parameters from event
        s3_bucket = event.get('s3_bucket')
        s3_key = event.get('s3_key')
        
        if not s3_bucket or not s3_key:
            return {
                'statusCode': 400,
                'body': json.dumps({
                    'error': 'Missing s3_bucket or s3_key in event'
                })
            }
        
        print(f"Starting migration from s3://{s3_bucket}/{s3_key}")
        
        # Get database connection string from Secrets Manager
        secrets_client = boto3.client('secretsmanager')
        db_secret_arn = os.environ.get('DB_SECRET_ARN')
        
        if not db_secret_arn:
            return {
                'statusCode': 500,
                'body': json.dumps({
                    'error': 'DB_SECRET_ARN environment variable not set'
                })
            }
        
        print(f"Fetching database secret from {db_secret_arn}")
        secret_response = secrets_client.get_secret_value(SecretId=db_secret_arn)
        db_secret = json.loads(secret_response['SecretString'])
        connection_string = db_secret.get('connectionString')
        
        if not connection_string:
            return {
                'statusCode': 500,
                'body': json.dumps({
                    'error': 'Connection string not found in secret'
                })
            }
        
        print("Database connection string retrieved successfully")
        
        # Download migration bundle from S3
        s3_client = boto3.client('s3')
        temp_dir = tempfile.mkdtemp()
        
        try:
            zip_path = os.path.join(temp_dir, 'migrations.zip')
            bundle_path = os.path.join(temp_dir, 'migrations-bundle')
            
            print(f"Downloading migration bundle from S3...")
            s3_client.download_file(s3_bucket, s3_key, zip_path)
            print(f"Downloaded to {zip_path}")
            
            # Extract the zip file
            print("Extracting migration bundle...")
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                zip_ref.extractall(temp_dir)
            
            # Find the migration bundle executable
            if not os.path.exists(bundle_path):
                # Look for the bundle in extracted files
                extracted_files = os.listdir(temp_dir)
                print(f"Extracted files: {extracted_files}")
                
                # The bundle might be in a subdirectory
                for root, dirs, files in os.walk(temp_dir):
                    for file in files:
                        if file == 'migrations-bundle' or file.endswith('bundle'):
                            bundle_path = os.path.join(root, file)
                            break
                    if bundle_path != os.path.join(temp_dir, 'migrations-bundle'):
                        break
            
            if not os.path.exists(bundle_path):
                return {
                    'statusCode': 500,
                    'body': json.dumps({
                        'error': f'Migration bundle not found in extracted files. Files: {os.listdir(temp_dir)}'
                    })
                }
            
            # Make the bundle executable
            os.chmod(bundle_path, 0o755)
            print(f"Found migration bundle at {bundle_path}")
            
            # Set environment variable for connection string
            # EF Core uses ConnectionStrings__DefaultConnection format
            env = os.environ.copy()
            env['ConnectionStrings__DefaultConnection'] = connection_string
            
            # Execute the migration bundle
            print("Executing migration bundle...")
            result = subprocess.run(
                [bundle_path, '--verbose'],
                env=env,
                capture_output=True,
                text=True,
                timeout=840  # 14 minutes (slightly less than Lambda timeout)
            )
            
            print(f"Migration bundle exit code: {result.returncode}")
            print(f"Migration stdout:\n{result.stdout}")
            if result.stderr:
                print(f"Migration stderr:\n{result.stderr}")
            
            if result.returncode != 0:
                return {
                    'statusCode': 500,
                    'body': json.dumps({
                        'error': 'Migration failed',
                        'exit_code': result.returncode,
                        'stdout': result.stdout,
                        'stderr': result.stderr
                    })
                }
            
            return {
                'statusCode': 200,
                'body': json.dumps({
                    'message': 'Migrations applied successfully',
                    'stdout': result.stdout
                })
            }
            
        finally:
            # Clean up temporary files
            shutil.rmtree(temp_dir, ignore_errors=True)
            print("Cleaned up temporary files")
            
    except subprocess.TimeoutExpired:
        return {
            'statusCode': 500,
            'body': json.dumps({
                'error': 'Migration timed out after 14 minutes'
            })
        }
    except Exception as e:
        print(f"Error: {str(e)}")
        import traceback
        traceback.print_exc()
        return {
            'statusCode': 500,
            'body': json.dumps({
                'error': str(e),
                'type': type(e).__name__
            })
        }
EOF
    filename = "index.py"
  }
}

# CloudWatch Log Group for migration Lambda
resource "aws_cloudwatch_log_group" "migration_lambda_log_group" {
  name              = "/aws/lambda/${aws_lambda_function.migration.function_name}"
  retention_in_days = 30
  tags              = local.common_tags
}

# Lambda permission for GitHub Actions to invoke
# The IAM policy on the deployer role/user already grants invoke permission via resource-based policy
# This resource-based permission allows cross-account or service-based invocations if needed
# For same-account IAM user/role invocations, the IAM policy is sufficient
resource "aws_lambda_permission" "migration_lambda_invoke" {
  statement_id  = "AllowDeployerInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.migration.function_name
  principal     = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"
  # This allows any IAM principal in the account to invoke (restricted by IAM policies)
}

data "aws_caller_identity" "current" {}
