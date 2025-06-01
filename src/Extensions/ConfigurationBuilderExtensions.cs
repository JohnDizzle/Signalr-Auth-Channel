using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration.Json;
//https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/security/key-vault-configuration/samples/6.x/KeyVaultConfigurationSample/Program.cs
//https://github.com/scottaddie/OnDotNetConfiguration/blob/main/src/Extensions/ConfigurationBuilderExtensions.cs
namespace AuthChannel.Extensions
{
    public static class ConfigurationBuilderExtensions
    {
        public static void ConfigureKeyVault(this IConfigurationBuilder config)
        {
            string? keyVaultEndpoint = Environment.GetEnvironmentVariable("KEYVAULT_ENDPOINT");

            if (keyVaultEndpoint is null)
                throw new InvalidOperationException("Store the Key Vault endpoint in a KEYVAULT_ENDPOINT environment variable.");

            var secretClient = new SecretClient(new(keyVaultEndpoint), new DefaultAzureCredential());
            config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }

        public static void WriteConfigurationSources(this IConfigurationBuilder config)
        {
            Console.WriteLine("Configuration sources\n=====================");
            foreach (var source in config.Sources)
            {
                if (source is JsonConfigurationSource jsonSource)
                    Console.WriteLine($"{source}: {jsonSource.Path}");
                else
                    Console.WriteLine(source.ToString());
            }
            Console.WriteLine("=====================\n");
        }
    }
}