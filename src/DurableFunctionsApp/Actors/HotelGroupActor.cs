using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using DurableFunctionsApp.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctionsApp.Actors
{
    [StorageAccount("AzureWebJobsStorage")]
    public static class HotelGroupActor
    {
        [FunctionName("HotelGroupActor")]
        public static async Task<HotelGroupActorState> Run(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            TraceWriter log)
        {
            HotelGroupActorState state = context.GetInput<HotelGroupActorState>();
            log.Info($"HotelGroupActor starting with refresh counter: {state.RefreshCounter} and isTest: {state.IsTest}");

            string operation = "refresh";
            using (var cts = new CancellationTokenSource())
            {
                var operationTask = context.WaitForExternalEvent<string>("operation");
                DateTime deadline = context.CurrentUtcDateTime.AddMinutes(state.MinutesBetweenInvocations);
                var timeoutTask = context.CreateTimer(deadline, cts.Token);

                Task winner = await Task.WhenAny(operationTask, timeoutTask);
                if (winner == operationTask)
                {
                    log.Info($"An operation event received!");
                    operation = operationTask.Result;
                    // Cancel the timer
                    cts.Cancel();
                }
                else
                {
                    // Default the timeout task to mean a 'refresh' operation
                    log.Info($"A timeout event received!");
                    operation = "refresh";
                }
            }

            log.Info($"***** received '{operation}' event.");

            operation = operation?.ToLowerInvariant();
            if (operation == "refresh")
            {
                state.UpTimeInMinutes += state.MinutesBetweenInvocations;

                log.Info($"Calling the refresh memberships function {state.UpTimeInMinutes}");
                var memberships = 0;
                memberships = await context.CallActivityAsync<int>("RefreshMemberships", state);

                state.RefreshCounter += memberships;
                log.Info($"Resuming after the refresh memberships function: {state.RefreshCounter}");

                string reason = $"Simulate an end due to no more memberships";
                if (memberships == 0)
                {
                    log.Info(reason);
                    state.EndReason = reason;
                    operation = "end";
                }
                else if (state.UpTimeInMinutes >= state.MaxMinutesToRun)
                {
                    reason = $"Simulate an end due to max run time reached";
                    log.Info(reason);
                    state.EndReason = reason;
                    operation = "end";
                }
            }
            else
            {
                // Support other operations
            }

            if (operation != "end")
            {
                log.Info($"Re-enqueing the state");
                context.ContinueAsNew(state);
            }

            if (operation == "end")
            {
                // Externalize the state to email, SMS or both - trigger a logic app
            }
 
            return state;
        }

        [FunctionName("RefreshMemberships")]
        public static async Task<int> RefreshMemberships([ActivityTrigger] HotelGroupActorState state, TraceWriter log)
        {
            var digitalMemberships = new List<string>();

            DateTime now = DateTime.Now;
            string formatDate = now.ToString("MM/dd/yyyy hh:mm:ss.fff tt");
            log.Info($"**** start refresh request for '{state.Code}' @ {formatDate}");

            // For testing only...
            if (state.RefreshCounter < state.MaxTestCounter)
                digitalMemberships.Add("TEST");

            now = DateTime.Now;
            formatDate = now.ToString("MM/dd/yyyy hh:mm:ss.fff tt");
            log.Info($"**** end refresh request for '{state.Code}' @ {formatDate}");
            return digitalMemberships.Count;
        }

        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
