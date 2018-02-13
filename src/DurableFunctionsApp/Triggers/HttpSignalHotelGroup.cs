using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using DurableFunctionsApp.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DurableFunctionsApp.Triggers
{
    public static class HttpSignalHotelGroup
    {
        [FunctionName("HttpSignalHotelGroup")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = "hotelgroups/{code}/signals/{signal}")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient context,
            string code,
            string signal,
            TraceWriter log)
        {
            string json = await req.Content.ReadAsStringAsync();
            HotelGroupActorState initialState = JsonConvert.DeserializeObject<HotelGroupActorState>(json);

            var hotelGroupActorStatus = await context.GetStatusAsync(code);
            string runningStatus = hotelGroupActorStatus == null ? "NULL" : hotelGroupActorStatus.RuntimeStatus.ToString();
            log.Info($"Instance running status: '{runningStatus}'.");

            if (hotelGroupActorStatus == null || hotelGroupActorStatus.RuntimeStatus != OrchestrationRuntimeStatus.Running)
            {
                await context.StartNewAsync("HotelGroupActor", code, initialState);
                log.Info($"Started a new hotel group actor with code = '{code}'.");
            }
            else
            {
                await context.RaiseEventAsync(code, "operation", signal);
                log.Info($"Signaled an existing hotel group actor with code '{code}' and signal '{signal}'.");
            }

            var res = context.CreateCheckStatusResponse(req, code);
            res.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
            return res;
        }
    }
}
