
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WebJobs.Script.Tests.Perf.Dashboard
{
    public static class RunPerfHttp
    {
        [FunctionName("RunPerfHttp")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation($"Performance tests were started by http trigger at: {DateTime.Now}");

                string testId = string.Empty;
                req.GetQueryParameterDictionary().TryGetValue("testId", out testId);

                await PerformanceManager.Execute(testId, log);

                return new ContentResult()
                {
                    Content = string.IsNullOrEmpty(testId) ? "All tests started" : $"Tests started: {testId}",
                    ContentType = "text/html"
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                return new ContentResult()
                {
                    Content = "Exception:" + ex,
                    ContentType = "text/html"
                };
            }
        }
    }
}
