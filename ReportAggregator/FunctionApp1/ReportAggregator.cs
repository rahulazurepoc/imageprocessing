using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
{
    public static class ReportAggregator
    {
        //wait for 15 minutes and then signal the ReportGenerator entity
        [FunctionName("ReportAggregator")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            [DurableClient] IDurableEntityClient client)
        {

            using (var cts = new CancellationTokenSource())
            {

                DateTime deadline = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(60));
                var fifteenMinuteTimer = context.CreateTimer(deadline,cts.Token);

                var printId = context.GetInput<string>();
                Console.WriteLine($"report aggregator called {DateTime.Now.ToString()} Print Id {printId}");
                bool shouldExit = false;
                
                var entityId = new EntityId("ReportGenerator", printId);

                do
                {
                    //call activity function to aggregate report
                    var isComplete = await context.CallActivityAsync<string>("RunReportAggregation", printId);
                    if (bool.Parse(isComplete))
                    {
                        shouldExit = true;
                        //notify entity all reports processed
                        cts.Cancel();

                        await context.CallActivityAsync<string>("NotifyReportCompletion", entityId);

                        break;
                    }
                    
                    //sleep for 30 seconds
                    DateTime miniDeadline = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(30));
                    var thirtySecondDelay = context.CreateTimer(deadline, cts.Token);

                    var winner = await Task.WhenAny(fifteenMinuteTimer, thirtySecondDelay);

                    if(winner == fifteenMinuteTimer)
                    {
                        //fifteen minute timeout
                        shouldExit = true;
                        //notify entity about timeout

                        await context.CallActivityAsync<string>("NotifyTimeout", entityId);

                        cts.Cancel();

                        break;
                    }


                } while (!shouldExit);

            }

            return;
        }
        
        [FunctionName("RunReportAggregation")]
        public static bool RunReportAggregation([ActivityTrigger] string printId
                                                    , [DurableClient] IDurableEntityClient client)
        {
            ////if all reports are generated return true else false
            //var random = new Random().Next(100);
            //if (random <= 50)
            //{
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}

            return true;

            ////15 minute timeout test, always return false
            //return false;
        }


        [FunctionName("NotifyReportCompletion")]
        public static async Task NotifyReportCompletion([ActivityTrigger] EntityId entityId
                                                    , [DurableClient] IDurableEntityClient client)
        {

            await client.SignalEntityAsync(entityId, "reportgenerated");

        }

        [FunctionName("NotifyTimeout")]
        public static async Task NotifyTimeout([ActivityTrigger] EntityId entityId
                                         , [DurableClient] IDurableEntityClient client)
        {

            await client.SignalEntityAsync(entityId, "timeout");
        }


    }
}