using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace AgroLink.Api;

public static class SecretsManagerHelper
{
    public static async Task LoadSecretsAsync(WebApplicationBuilder builder)
    {
        var secretArn = Environment.GetEnvironmentVariable("AgroLink__DbSecretArn");

        if (string.IsNullOrEmpty(secretArn))
        {
            // Not running in AWS or env var not set
            return;
        }

        try
        {
            var secretsClient = new AmazonSecretsManagerClient();
            var request = new GetSecretValueRequest { SecretId = secretArn };

            var response = await secretsClient.GetSecretValueAsync(request);

            if (response.SecretString != null)
            {
                var secretJson = JsonDocument.Parse(response.SecretString);
                string connectionString;

                if (secretJson.RootElement.TryGetProperty("connectionString", out var connStr))
                {
                    connectionString = connStr.GetString() ?? "";
                    if (
                        !string.IsNullOrEmpty(connectionString)
                        && !connectionString.Contains("Timeout=")
                    )
                    {
                        connectionString += ";Timeout=60;CommandTimeout=60";
                    }
                }
                else
                {
                    // Fallback: Build from fields
                    var host = secretJson.RootElement.GetProperty("host").GetString();
                    var port = secretJson.RootElement.GetProperty("port").GetInt32();
                    var database = secretJson.RootElement.GetProperty("database").GetString();
                    var username = secretJson.RootElement.GetProperty("username").GetString();
                    var password = secretJson.RootElement.GetProperty("password").GetString();

                    connectionString =
                        $"Host={host};Port={port};Database={database};Username={username};Password={password};Timeout=60;CommandTimeout=60";
                }

                // Add to configuration
                builder.Configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "ConnectionStrings:DefaultConnection", connectionString },
                    }
                );

                Console.WriteLine(
                    "Successfully loaded database connection string from Secrets Manager."
                );
            }
        }
        catch (Exception ex)
        {
            // Note: We keep Console.WriteLine here because this runs during startup
            // before the logging system is fully initialized.
            Console.WriteLine($"Error loading secrets: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
