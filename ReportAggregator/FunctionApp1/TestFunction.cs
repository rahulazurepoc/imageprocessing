using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace FunctionApp1
{
    public static class TestFunction
    {
        [FunctionName("TestFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableEntityClient client,
            ILogger log)
        {
            var printId = req.Query["printId"].ToString();

            // Entity operation input comes from the queue message content.
            var entityId = new EntityId("ReportGenerator", printId);
            await client.SignalEntityAsync(entityId, "GenerateReport", printId);

            return new OkObjectResult("success");
        }
    }
}
