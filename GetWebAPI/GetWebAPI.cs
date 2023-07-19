using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using System.ComponentModel;
using Azure;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GetWebAPI
{
    public static class GetWebAPI
    {
        private static readonly string cosmosDbEndpoint = Environment.GetEnvironmentVariable("DBENDPOINT", EnvironmentVariableTarget.Process);
        private static readonly string cosmosDbKey = Environment.GetEnvironmentVariable("DBKEY", EnvironmentVariableTarget.Process);
        private static readonly string databaseId = Environment.GetEnvironmentVariable("DBID", EnvironmentVariableTarget.Process);
        private static readonly string containerId = Environment.GetEnvironmentVariable("CONTAINERID", EnvironmentVariableTarget.Process);
        private static readonly CosmosClient cosmosClient = new CosmosClient(cosmosDbEndpoint, cosmosDbKey);
        private static readonly Microsoft.Azure.Cosmos.Container cosmosContainer = cosmosClient.GetContainer(databaseId, containerId);

        [FunctionName("GetWebAPI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                string id = req.Query["id"];

                // Validate that the ID is provided
                if (string.IsNullOrEmpty(id))
                {
                    return new BadRequestObjectResult("The 'id' query parameter is required.");
                }

                // Retrieve the item from Cosmos DB
                var response = await cosmosContainer.ReadItemAsync<RequestDocument>(id, new PartitionKey(id));

                return new OkObjectResult(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while retrieving data from Cosmos DB.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }

    public class RequestDocument
    {
        public string id { get; set; }
        public string categoryId { get; set; }
        public string categoryName { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public double Price { get; set; }
        public List<Tag> tags { get; set; }
    }

    public class Tag
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}
