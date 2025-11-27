# AgroLink Infrastructure as Code (Terraform)

This repository provisions the AgroLink backend infrastructure in AWS using Terraform and includes a Docker-based local development setup.

## What this stack provisions

- Networking
  - VPC with DNS support/hostnames
  - Two private subnets
  - Security groups: `lambda_sg`, `migration_lambda_sg`, `rds_sg`, `vpce_sg`
  - Default route table
  - VPC endpoints: Secrets Manager (Interface), S3 (Gateway)

- Compute
  - AWS Lambda function `AgroLinkAPI-AspNetCoreFunction` (arm64)
  - Migration Lambda function `AgroLink-Migration-Function` (runs EF Core migrations in VPC)
  - CloudWatch Log Groups for both functions (30-day retention)

- API and Edge
  - API Gateway REST API with ANY proxy and root methods, Lambda proxy integrations
  - Stages: `Prod` and `Stage` with:
    - Access logging to CloudWatch Logs (JSON)
    - X-Ray tracing enabled
  - Lambda invoke permissions for API Gateway
  - AWS WAFv2 Web ACL with AWS managed rules and a rate-limit rule, associated to API Gateway `Prod`
  - CloudFront distribution in front of the SPA S3 bucket
    - Origin Access Control (OAC) to securely access bucket
    - 404 -> index.html for SPA routing

- Security/IAM
  - Lambda execution role with:
    - Basic execution and VPC access managed policies
    - Permission to read app DB secret and KMS decrypt (scope to your key as needed)
  - CI deploy users (optional) with scoped policies:
    - `Lambda_Code_Deployer` (upload Lambda artifacts and update Lambda)
    - `SPA_Deployer` (upload SPA artifacts)
  - KMS Key for RDS encryption

- Data
  - Aurora PostgreSQL Serverless v2 cluster and instance
    - Deletion protection enabled
    - Serverless scaling config (auto pause)
    - DB subnet group
  - Secrets Manager secrets
    - JWT secret key (randomly generated)
    - Aggregated DB connection secret (host, port, db, user, password, connection string)

- Storage/CDN
  - S3 buckets
    - Lambda code bucket (versioning + lifecycle for old versions)
    - SPA bucket (private; readable only by CloudFront OAC)
    - File storage bucket
  - ACM certificate (DNS validated) for `<domain>` and `www.<domain>`
  - Route 53 A-records for apex and `www` pointing to CloudFront

- Monitoring and Ops
  - CloudWatch alarms: Lambda Errors, Lambda Duration, RDS CPU, RDS Connections
  - CloudWatch dashboard (Lambda + RDS widgets)

- Outputs
  - `rds_endpoint`
  - `spa_distribution_domain_name`
  - `lambda_deployer_access_key_id` / `lambda_deployer_secret_access_key` (sensitive)
  - `spa_deployer_access_key_id` / `spa_deployer_secret_access_key` (sensitive)

## Variables (highlights)

- Region and tagging
  - `region` (default `us-east-1`)
  - Central tags via `local.common_tags`: `Service=AgroLink`, `Environment=Production`

- Lambda artifact selection
  - `use_placeholder` (default `true`): if true, deploys a tiny Python placeholder zipped and uploaded automatically
  - `lambda_package_key`: S3 key for your real artifact zip
  - `code_bucket_name`: S3 bucket that holds Lambda artifacts
  - `enable_lambda_vpc` (default `true`): attach Lambda to VPC (set `false` for simple placeholder testing)

- Database
  - `db_identifier`, `db_name`, `db_master_username`

- SPA / Storage buckets
  - `spa_bucket_name`, `storage_bucket_name`

- Domain
  - `domain_name` (apex) and hosted zone lookups for ACM/Route53

## Deploy

```bash
terraform init
terraform plan
terraform apply
```

### Placeholder vs real Lambda code

- Placeholder (default):
  - `use_placeholder = true`
  - A minimal Python handler is zipped locally and uploaded to `code_bucket_name` at `lambda_placeholder_key`.
- Real code from your CI upload:
  - Ensure your CI uploads the zip to `s3://<code_bucket_name>/<lambda_package_key>`
  - Set:
    ```hcl
    use_placeholder    = false
    lambda_package_key = "AgroLink.API/AgroLinkAPI-AspNetCoreFunction-<build>.zip"
    enable_lambda_vpc  = true # if the app talks to DB
    ```
  - Apply again.

## Local development (Docker)

Services:
- Postgres 16 (port 5432)
- pgAdmin (elestio/pgadmin) at http://localhost:5050 (admin@example.com / admin1234)
- MinIO S3-compatible storage:
  - API: http://localhost:9000
  - Console: http://localhost:9001 (user `minio`, password `minio12345`)
  - Buckets auto-created: `agrolink-spa-bucket`, `agrolink-storage-bucket`

Usage:
```bash
docker compose up -d --pull always
```

Point your local app’s S3 client to MinIO:
- Endpoint: `http://localhost:9000`
- Access key: `minio`
- Secret key: `minio12345`
- Region: `us-east-1` (arbitrary)

Database connection for local app:
- `postgres://agrolinkadmin:agrolinkpassword@localhost:5432/agrolinkdb`

## Post-deploy checks

- API Gateway: confirm stages exist and access logs are being written
- Lambda: test invoke and check logs in `/aws/lambda/AgroLinkAPI-AspNetCoreFunction`
- CloudFront: browse the distribution domain and verify SPA loads
- S3: direct object access should be denied; CloudFront should serve content
- RDS: connect using the output endpoint and DB credentials (from Secrets Manager in AWS)

## Cleanup (warning)

- RDS has `deletion_protection = true`. Disable it first if you intend to destroy the DB:
  1. Update variable/code to set deletion protection to false
  2. `terraform apply`
  3. `terraform destroy`

## ⚠️ IMPORTANT: Database Migrations

**Migrations are handled by GitHub Actions CI/CD**, not during Lambda startup. 

**Ensure `Database.Migrate()` is removed from `Program.cs`** to prevent Lambda initialization timeouts.

**GitHub Actions runs migrations before deployment using a dedicated Lambda function:**
```bash
# GitHub Actions generates migration bundle and uploads to S3
dotnet ef migrations bundle --runtime linux-arm64 --output ./migrations-bundle
aws s3 cp migrations-bundle.zip s3://bucket/migrations/migrations-abc123.zip

# Then invokes the migration Lambda function
aws lambda invoke \
  --function-name AgroLink-Migration-Function \
  --payload '{"s3_bucket":"bucket","s3_key":"migrations/migrations-abc123.zip"}' \
  response.json
```

The migration Lambda function (`AgroLink-Migration-Function`) is provisioned by Terraform and runs EF Core migrations in the VPC before Lambda deployment.

## Database Connection via Secrets Manager

The Lambda function connects **directly** to RDS and reads the database connection string from AWS Secrets Manager at runtime. The secret ARN is provided via the `AgroLink__DbSecretArn` environment variable.

### Architecture

```
Lambda → Aurora PostgreSQL (direct connection)
         ↑
    Secrets Manager (credentials)
```

### Why Direct Connection?

**Pros:**
- ✅ **Simpler setup** - No additional service to manage
- ✅ **Lower cost** - No RDS Proxy fees (~$15/month saved)
- ✅ **Lower latency** - One less hop in the network path
- ✅ **Full control** - Direct connection to your database

**Cons & Considerations:**
- ⚠️ **No connection pooling** - Each Lambda invocation may create a new connection
- ⚠️ **Connection overhead** - More connections to RDS under high load
- ⚠️ **Cold start impact** - First connection after database pause can be slower
- ⚠️ **Connection limits** - Need to monitor RDS max connections

**Best Practices for Direct Connections:**
1. **Use Entity Framework connection pooling** - EF Core pools connections within the Lambda execution context
2. **Cache connections** - Reuse DbContext instances within the same Lambda invocation
3. **Set appropriate timeouts** - Configure connection timeout and command timeout
4. **Monitor connections** - Watch CloudWatch metrics for connection count
5. **Consider Aurora Serverless v2 scaling** - Ensure `min_capacity` prevents frequent pauses

### Secret Format

The secret `agro_link_db_connection` contains JSON with:
- Individual fields: `host`, `port`, `database`, `username`, `password`
- Ready-to-use connection string: `connectionString` (PostgreSQL/Npgsql format)

### Reading the Secret in .NET Lambda

Example code to read the secret in your ASP.NET Core Lambda:

```csharp
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

// In your Startup/Program.cs or configuration setup
public class SecretsManagerConfigurationProvider
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly string _secretArn;

    public SecretsManagerConfigurationProvider(IAmazonSecretsManager secretsManager, string secretArn)
    {
        _secretsManager = secretsManager;
        _secretArn = secretArn;
    }

    public async Task<string> GetConnectionStringAsync()
    {
        var request = new GetSecretValueRequest
        {
            SecretId = _secretArn
        };

        var response = await _secretsManager.GetSecretValueAsync(request);
        var secretJson = JsonDocument.Parse(response.SecretString);
        
        // Use the ready-to-use connection string
        if (secretJson.RootElement.TryGetProperty("connectionString", out var connStr))
        {
            return connStr.GetString();
        }
        
        // Fallback: Build it from individual fields
        var host = secretJson.RootElement.GetProperty("host").GetString();
        var port = secretJson.RootElement.GetProperty("port").GetInt32();
        var database = secretJson.RootElement.GetProperty("database").GetString();
        var username = secretJson.RootElement.GetProperty("username").GetString();
        var password = secretJson.RootElement.GetProperty("password").GetString();
        
        return $"Host={host};Port={port};Username={username};Password={password};Database={database}";
    }
}

// In your DbContext configuration (e.g., Program.cs)
var secretArn = Environment.GetEnvironmentVariable("AgroLink__DbSecretArn");
var secretsManager = new AmazonSecretsManagerClient();
var configProvider = new SecretsManagerConfigurationProvider(secretsManager, secretArn);
var connectionString = await configProvider.GetConnectionStringAsync();

services.AddDbContext<YourDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable connection pooling (EF Core default, but good to be explicit)
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));
```

**Important Notes:**
1. **Cache the secret value** during Lambda execution to avoid repeated API calls. Consider using a static variable or dependency injection singleton.
2. **EF Core connection pooling** works within a single Lambda invocation - connections are pooled and reused automatically.
3. **For high-concurrency scenarios**, consider implementing a shared connection pool or upgrading to RDS Proxy if you experience connection limit issues.

### NuGet Packages Required

```xml
<PackageReference Include="AWSSDK.SecretsManager" Version="3.7.400" />
```

## Notes / Recommendations

- GitHub Actions uses IAM user access keys for deployment (stored as environment secrets)
- Scope the Lambda role’s KMS decrypt to the specific key ARN instead of `*`
- Use a stable Lambda artifact key (e.g., `AgroLink.API/latest.zip`) and let CI copy the build to it
