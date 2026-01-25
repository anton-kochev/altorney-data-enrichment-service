using Application.Configuration;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Http.Timeouts;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Get endpoint configuration for request size limits and timeouts
EnrichmentEndpointOptions endpointOptions = builder.Configuration
    .GetSection(EnrichmentEndpointOptions.SectionName)
    .Get<EnrichmentEndpointOptions>() ?? new EnrichmentEndpointOptions();

// Configure Kestrel request size limits
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = endpointOptions.MaxRequestSizeBytes;
});

// Configure request timeouts
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("EnrichmentPolicy", new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(endpointOptions.TimeoutSeconds),
        WriteTimeoutResponse = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Request timed out");
        }
    });
});

// Add services to the container.
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add controllers - CSV formatters are configured via IConfigureOptions<MvcOptions> from Infrastructure
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable request timeouts (must be before UseAuthorization)
app.UseRequestTimeouts();

app.UseAuthorization();

app.MapControllers();

app.Run();
