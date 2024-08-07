namespace PrivacyTagApp
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Microsoft.DataGrid.GatewayV2.Client;
    using Microsoft.DataGrid.GatewayV2.Contracts;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Web;

    internal class Program
    {
        static void Main(string[] args)
        {
            var client = Task.Run(async () => await ConfigureClientAppAsync()).GetAwaiter().GetResult();

            EntityQuery query = new EntityQuery();
            query.SetKustoMartFilters("convergedrecordemea", "EngineeringExpDoc", "P2PKPIProd");

            var searchResult = client.SearchEntitiesAsync(query).Result;

            foreach (var entity in searchResult.EntityDescriptors)
            {
                Debug.WriteLine("Found entity named '" + entity.Name + "' of type " + entity.EntityType + " with identity '" + entity.Identifier + "'.");
            }

            Debug.WriteLine("Hello, World!");
        }

        private static async Task<GatewayClient> ConfigureClientAppAsync()
        {
            var clientId = "23f5340c-2702-4442-aa2c-926bb659ffd2";
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = clientId
            };

            var defaultAzureCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);

            // NOTE: By default, the DefaultAzureCredential will manage the caching and refresh token
            var result = await defaultAzureCredential.GetTokenAsync(new TokenRequestContext(new string[] { "api://AzureADTokenExchange/.default" }));

            var confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority("https://login.microsoftonline.com/microsoft.onmicrosoft.com")
                .WithClientAssertion(() => result.Token)
                .Build();

            var token = await confidentialClient.AcquireTokenForClient(new string[] { "api://datagridgatewayserviceprod/.default" }).ExecuteAsync();
            var client = new GatewayClient(new Uri("https://gateway.datagridservices.microsoft.com"), "OsgDna");
            await client.SetCredentialsAsync(token.AccessToken);

            return client;
        }
    }
}
