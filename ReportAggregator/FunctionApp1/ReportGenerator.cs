using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
{
    public static class ReportGeneratorFunc
    {
        [FunctionName("ReportGenerator")]
        public static void ReportGenerator(
            [EntityTrigger] IDurableEntityContext context,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            switch (context.OperationName.ToLower())
            {
                case "generatereport":

                    Console.WriteLine($"Generate report called {DateTime.Now.ToString()}");

                    var reportAggregationStarted = context.GetState<bool>();
                    if (!reportAggregationStarted)
                    {
                        var printId = context.GetInput<string>();
                        string instanceId = 
                            starter.StartNewAsync("ReportAggregator", null,printId).Result;


                        context.SetState(true);

                    }



                    break;
                case "timeout":

                    Console.WriteLine($"Timeout {DateTime.Now.ToString()}");
                    
                    //notify caller
                    break;
                case "reportgenerated":

                    Console.WriteLine($"report generated {DateTime.Now.ToString()}");


                    //notify caller
                    break;
            }


        }

        [FunctionName("ReportGenerator_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("ReportGenerator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ReportGenerator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}