using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;

namespace AgroLink.Infrastructure;

public static class SecretsManagerHelper
{
    public static async Task LoadSecretsAsync(IConfigurationBuilder configBuilder)
    {
        var secretArn = Environment.GetEnvironmentVariable("AgroLink__DbSecretArn");

        if (string.IsNullOrEmpty(secretArn))
        {
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
                    var host = secretJson.RootElement.GetProperty("host").GetString();
                    var port = secretJson.RootElement.GetProperty("port").GetInt32();
                    var database = secretJson.RootElement.GetProperty("database").GetString();
                    var username = secretJson.RootElement.GetProperty("username").GetString();
                    var password = secretJson.RootElement.GetProperty("password").GetString();

                    connectionString =
                        $"Host={host};Port={port};Database={database};Username={username};Password={password};Timeout=60;CommandTimeout=60";
                }

                configBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "ConnectionStrings:DefaultConnection", connectionString },
                    }
                );

                Console.WriteLine("[SecretsManagerHelper] Loaded database connection string from Secrets Manager.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SecretsManagerHelper] Error loading secrets from {secretArn}: {ex.Message}");
        }
    }
}
