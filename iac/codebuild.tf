# Security group for CodeBuild - allows access to RDS and VPC endpoints
resource "aws_security_group" "codebuild_sg" {
  vpc_id = aws_vpc.main.id

  # No ingress needed - CodeBuild initiates connections

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.common_tags, { Name = "codebuild-sg" })
}

# Update RDS security group to allow CodeBuild access
resource "aws_security_group_rule" "rds_allow_codebuild" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.codebuild_sg.id
  security_group_id        = aws_security_group.rds_sg.id
  description              = "Allow CodeBuild to connect to RDS for migrations"
}

# IAM role for CodeBuild
resource "aws_iam_role" "codebuild_role" {
  name = "AgroLink-CodeBuild-Migration-Role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "codebuild.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })

  tags = merge(local.common_tags, {
    Name = "CodeBuild Migration Role"
  })
}

# IAM policy for CodeBuild - S3, Secrets Manager, CloudWatch Logs, VPC access
resource "aws_iam_role_policy" "codebuild_policy" {
  role = aws_iam_role.codebuild_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:GetObjectVersion",
          "s3:ListBucket"
        ]
        Resource = [
          aws_s3_bucket.lambda_code_bucket.arn,
          "${aws_s3_bucket.lambda_code_bucket.arn}/*",
          # Allow access to migrations bucket (if different from lambda bucket)
          "arn:aws:s3:::agrolink-migrations",
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
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:${var.region}:*:log-group:/aws/codebuild/AgroLink-Migrations:*"
      },
      {
        Effect = "Allow"
        Action = [
          "ec2:CreateNetworkInterface",
          "ec2:DescribeNetworkInterfaces",
          "ec2:DeleteNetworkInterface",
          "ec2:DescribeSubnets",
          "ec2:DescribeSecurityGroups",
          "ec2:DescribeVpcs",
          "ec2:DescribeVpcAttribute",
          "ec2:DescribeDhcpOptions",
          "ec2:DescribeRouteTables",
          "ec2:DescribeInternetGateways"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "ec2:CreateNetworkInterfacePermission"
        ]
        Resource = "arn:aws:ec2:${var.region}:*:network-interface/*"
        Condition = {
          StringEquals = {
            "ec2:Subnet" = [
              for subnet in aws_subnet.private : subnet.arn
            ]
            "ec2:AuthorizedService" = "codebuild.amazonaws.com"
          }
        }
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

# Attach VPC access policy to CodeBuild role
resource "aws_iam_role_policy_attachment" "codebuild_vpc_access" {
  role       = aws_iam_role.codebuild_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

# CodeBuild project for running database migrations
resource "aws_codebuild_project" "db_migrations" {
  name          = "AgroLink-DB-Migrations"
  description   = "Runs EF Core database migrations in VPC before Lambda deployment"
  service_role  = aws_iam_role.codebuild_role.arn
  build_timeout = 20 # 20 minutes timeout

  artifacts {
    type = "NO_ARTIFACTS"
  }

  environment {
    compute_type                = "BUILD_GENERAL1_SMALL"
    image                       = "aws/codebuild/standard:7.0" # .NET 8 support
    type                        = "LINUX_CONTAINER"
    image_pull_credentials_type = "CODEBUILD"
    privileged_mode             = false

    environment_variable {
      name  = "AWS_DEFAULT_REGION"
      value = var.region
    }

    # S3_BUCKET and S3_KEY will be provided when starting the build from GitHub Actions
    # Default to lambda_code_bucket, but can be overridden
    environment_variable {
      name  = "S3_BUCKET"
      value = aws_s3_bucket.lambda_code_bucket.bucket
      type  = "PLAINTEXT"
    }

    # S3_KEY will be provided when starting the build from GitHub Actions
    # Example: aws codebuild start-build --project-name AgroLink-DB-Migrations \
    #   --environment-variables-override name=S3_KEY,value=migrations/migrations-123.zip,name=S3_BUCKET,value=agrolink-migrations
  }

  # Configure VPC access
  vpc_config {
    vpc_id             = aws_vpc.main.id
    subnets            = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.codebuild_sg.id]
  }

  # NO_SOURCE - we'll download artifacts from S3 in the buildspec
  source {
    type      = "NO_SOURCE"
    buildspec = <<-EOF
version: 0.2
phases:
  pre_build:
    commands:
      - echo "Downloading EF migration bundle from S3..."
      - aws s3 cp s3://$${S3_BUCKET}/$${S3_KEY} ./db-migrate.zip
      - rm -rf ./db-migrate
      - mkdir -p ./db-migrate
      - unzip -q db-migrate.zip -d ./db-migrate
      - echo "Contents of ./db-migrate:"
      - ls -la ./db-migrate
      # Make the bundle executable
      - chmod +x ./db-migrate/migrations-bundle || true

  build:
    commands:
      - echo "Fetching secrets from Secrets Manager..."
      - |
        DB_SECRET_ARN="${aws_secretsmanager_secret.agro_link_db_connection.arn}"
        if [ -z "$DB_SECRET_ARN" ] || [ "$DB_SECRET_ARN" = "" ]; then
          echo "ERROR: DB_SECRET_ARN is not set!"
          exit 1
        fi
        aws secretsmanager get-secret-value --secret-id "$DB_SECRET_ARN" --query SecretString --output text > db.json

        JWT_SECRET_ARN="${aws_secretsmanager_secret.jwt_secret_key.arn}"

        if [ -z "$JWT_SECRET_ARN" ] || [ "$JWT_SECRET_ARN" = "" ]; then
          echo "ERROR: JWT_SECRET_ARN is not set!"
          exit 1
        fi
        JWT_SECRET=$(aws secretsmanager get-secret-value --secret-id "$JWT_SECRET_ARN" --query SecretString --output text)
        export JWT__Secret="$JWT_SECRET"

      - |
        # Extract connection string and set environment variables
        echo "=== Database Connection Debugging ==="
        echo "Secret JSON contents:"
        cat db.json | jq '.'
        
        CONNECTION_STRING=$(jq -r '.connectionString' db.json)
        DB_HOST=$(jq -r '.host' db.json)
        DB_PORT=$(jq -r '.port' db.json)
        DB_CLUSTER_ID="${var.db_identifier}"
        
        echo ""
        echo "Extracted values:"
        echo "  Host: $DB_HOST"
        echo "  Port: $DB_PORT"
        echo "  Cluster ID: $DB_CLUSTER_ID"
        echo "  Connection String: $CONNECTION_STRING"
        echo ""
        
        # Verify RDS cluster status and wait if paused
        echo "=== Checking RDS Cluster Status ==="
        MAX_WAIT=300  # 5 minutes max wait
        ELAPSED=0
        INTERVAL=10
        
        while [ $ELAPSED -lt $MAX_WAIT ]; do
          CLUSTER_STATUS=$(aws rds describe-db-clusters \
            --db-cluster-identifier "$DB_CLUSTER_ID" \
            --query 'DBClusters[0].Status' \
            --output text 2>/dev/null || echo "unknown")
          
          echo "RDS Cluster Status: $CLUSTER_STATUS (elapsed: ${ELAPSED}s)"
          
          if [ "$CLUSTER_STATUS" = "available" ]; then
            echo "✓ RDS cluster is available"
            break
          elif [ "$CLUSTER_STATUS" = "starting" ] || [ "$CLUSTER_STATUS" = "backing-up" ]; then
            echo "⏳ RDS cluster is $CLUSTER_STATUS, waiting..."
            sleep $INTERVAL
            ELAPSED=$((ELAPSED + INTERVAL))
          else
            echo "⚠️  Unexpected RDS status: $CLUSTER_STATUS"
            echo "Proceeding anyway, but migration may fail..."
            break
          fi
        done
        
        if [ $ELAPSED -ge $MAX_WAIT ]; then
          echo "❌ Timeout waiting for RDS to become available"
          exit 1
        fi
        
        # Verify connection string format
        if echo "$CONNECTION_STRING" | grep -q "tcp://"; then
          echo "WARNING: Connection string contains 'tcp://' prefix - this is unusual for PostgreSQL!"
        fi
        
        # Test DNS resolution
        echo ""
        echo "Testing DNS resolution for $DB_HOST..."
        if host "$DB_HOST" > /dev/null 2>&1; then
          echo "✓ DNS resolution successful"
          host "$DB_HOST" | head -3
        else
          echo "✗ DNS resolution failed!"
          echo "Trying with nslookup..."
          nslookup "$DB_HOST" || echo "nslookup also failed"
        fi
        
        # Test network connectivity with retry
        echo ""
        echo "Testing network connectivity to $DB_HOST:$DB_PORT..."
        CONNECTIVITY_RETRIES=3
        CONNECTIVITY_SUCCESS=false
        
        for i in $(seq 1 $CONNECTIVITY_RETRIES); do
          if command -v nc > /dev/null 2>&1; then
            if timeout 10 nc -zv "$DB_HOST" "$DB_PORT" 2>&1; then
              echo "✓ Network connectivity test PASSED (attempt $i)"
              CONNECTIVITY_SUCCESS=true
              break
            else
              echo "✗ Network connectivity test FAILED (attempt $i/$CONNECTIVITY_RETRIES)"
              if [ $i -lt $CONNECTIVITY_RETRIES ]; then
                echo "  Retrying in 5 seconds..."
                sleep 5
              fi
            fi
          else
            echo "nc (netcat) not available, skipping connectivity test"
            CONNECTIVITY_SUCCESS=true  # Skip if tool not available
            break
          fi
        done
        
        if [ "$CONNECTIVITY_SUCCESS" = false ]; then
          echo "❌ Network connectivity test failed after $CONNECTIVITY_RETRIES attempts"
          echo "This could indicate:"
          echo "  - Security group rules not allowing traffic"
          echo "  - Database is still pausing/starting"
          echo "  - Network routing issue"
          exit 1
        fi
        
        # Test with psql if available (just connection test, not actual query)
        if command -v psql > /dev/null 2>&1; then
          echo ""
          echo "Testing PostgreSQL connection..."
          PGPASSWORD=$(jq -r '.password' db.json) timeout 10 psql \
            -h "$DB_HOST" \
            -p "$DB_PORT" \
            -U "$(jq -r '.username' db.json)" \
            -d "$(jq -r '.database' db.json)" \
            -c "SELECT 1;" 2>&1 | head -5 || echo "psql connection test failed"
        fi
        
        echo ""
        echo "=== Setting environment variables ==="
        export ConnectionStrings__AgroLinkDB="$CONNECTION_STRING"
        echo "  ConnectionStrings__AgroLinkDB: [set]"
        echo "  JWT__Secret: [set]"
        echo ""
        echo "Running EF Core migrations..."

      - |
        echo "Checking for migration files..."
        ls -la ./db-migrate

        if [ -f ./db-migrate/migrations-bundle ]; then
          echo "Found EF migrations bundle (migrations-bundle)"
          echo "Executing migrations bundle with environment variables..."
          # The migrations bundle should pick up ConnectionStrings__AgroLinkDB and JWT__Secret from environment
          ./db-migrate/migrations-bundle
        else
          echo "ERROR: migrations-bundle not found under ./db-migrate"
          echo "Available files:"
          find ./db-migrate -type f | head -20
          exit 1
        fi

  post_build:
    commands:
      - echo "Migration completed successfully"
    EOF
  }

  logs_config {
    cloudwatch_logs {
      group_name  = "/aws/codebuild/AgroLink-Migrations"
      stream_name = "migration-builds"
    }
  }

  tags = merge(local.common_tags, {
    Name = "DB Migrations CodeBuild Project"
  })
}

# CloudWatch Log Group for CodeBuild logs
resource "aws_cloudwatch_log_group" "codebuild_migrations" {
  name              = "/aws/codebuild/AgroLink-Migrations"
  retention_in_days = 30
  tags              = local.common_tags
}
