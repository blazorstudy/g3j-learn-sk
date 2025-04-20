using FeedbackAgent;
using FeedbackAgent.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FeedbackServiceFunctions
{
    public class FeedbackServiceFunctions
    {
        private readonly ILogger<FeedbackServiceFunctions> _logger;

        public FeedbackServiceFunctions(ILogger<FeedbackServiceFunctions> logger)
        {
            _logger = logger;
        }

        [Function("ReportFeedback")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);
            var report = JsonSerializer.Deserialize<Feedback>(requestBody);

            var feedbackReportService = new FeedbackReportService();
            report = await feedbackReportService.Report(report);

            return new OkObjectResult(report);
        }
    }
}
