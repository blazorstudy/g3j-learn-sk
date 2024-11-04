using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Add Semantic Kernel as singleton instance.
builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var kernel = Kernel.CreateBuilder()
                       .AddAzureOpenAIChatCompletion(
                           endpoint: config["OpenAI:Endpoint"]!,
                           apiKey: config["OpenAI:ApiKey"]!,
                           deploymentName: config["OpenAI:DeploymentName"]!)
                       .Build();
    return kernel;
});
builder.Services.AddTransient<KernelClient>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithTags("weather")
.WithName("GetWeatherForecast")
.WithOpenApi();



// Add endpoint for chat completion through Semantic Kernel
app.MapPost("/chat/complete", async ([FromBody] Request req, KernelClient client) =>
{
    var result = await client.CompleteChatAsync(req.Prompt);

    return new Response(result);
})
.Accepts<Request>(contentType: "application/json")
.Produces<Response>(statusCode: StatusCodes.Status200OK, contentType: "application/json")
.WithTags("openai")
.WithName("CompleteChat")
.WithOpenApi();




app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

class KernelClient(Kernel kernel)
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public async Task<string> CompleteChatAsync(string prompt)
    {
        var result = await _kernel.InvokePromptAsync(prompt).ConfigureAwait(false);

        return result!.GetValue<string>()!;
    }
}

record Request(string Prompt);
record Response(string Completion);