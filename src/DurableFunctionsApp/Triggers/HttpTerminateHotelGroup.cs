using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctionsApp.Triggers
{
    public static class HttpTerminateHotelGroup
    {
        [FunctionName("HttpTerminateHotelGroup")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = "hotelgroups/{code}/terminate")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient context,
            string code,
            TraceWriter log)
        {
            try
            {
                log.Info($"Terminating instance '{code}'....");
                // Not sure why terminating causes a timeout event at the actor!!! Question in forums
                await context.TerminateAsync(code, "Via an API request");
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
    }
}
