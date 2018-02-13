using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctionsApp.Triggers
{
    public static class HttpGetHotelGroupStatus
    {
        [FunctionName("HttpGetHotelGroupStatus")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, methods: "get", Route = "hotelgroups/{code}/status")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient context,
            string code,
            TraceWriter log)
        {
            var status = await context.GetStatusAsync(code);
            if (status != null)
            {
                return req.CreateResponse<dynamic>(HttpStatusCode.OK, status);
            }
            else
            {
                return req.CreateResponse<dynamic>(HttpStatusCode.BadRequest, $"{code} hotel group actor is not found!");
            }
        }
    }
}
