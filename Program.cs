using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using System.Reflection;
using WebApi.Controllers;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var builder = WebApplication.CreateBuilder(args);

string url = config.GetSection("AppSettings")["OTLService"]; //"http://jaeger:4317";
// Configure metrics
builder.Services.AddOpenTelemetryMetrics(build =>
{
    build.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName));
    build.AddHttpClientInstrumentation();
    build.AddAspNetCoreInstrumentation();
    build.AddMeter(builder.Environment.ApplicationName);
    build.AddOtlpExporter(options => options.Endpoint = new Uri(url));
    build.AddConsoleExporter();

});
// Configure tracing
builder.Services.AddOpenTelemetryTracing(build =>
{
    build.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName)).AddSource(nameof(SolicitacaoController));
    build.AddHttpClientInstrumentation();
    build.AddAspNetCoreInstrumentation();
    build.AddSqlClientInstrumentation(options => { options.SetDbStatementForText = true; options.RecordException = true; });
    build.AddOtlpExporter(options => options.Endpoint = new Uri(url));
});

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddOpenTelemetry(build =>
    {
        build.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName));
        build.IncludeFormattedMessage = true;
        build.IncludeScopes = true;
        build.ParseStateValues = true;
        build.AddOtlpExporter(options => options.Endpoint = new Uri(url));
        build.AddConsoleExporter();
    });
});

// // Configure logging
// builder.Logging.AddOpenTelemetry(build =>
// {
//     build.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName));
//     build.IncludeFormattedMessage = true;
//     build.IncludeScopes = true;
//     build.ParseStateValues = true;
//     build.AddOtlpExporter(options => options.Endpoint = new Uri(url));
//     build.AddConsoleExporter();
// });
builder.Services.AddHttpClient();
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v1",
                    Title = "WebApi",
                    Description = "Exemplo WebApi VS Code",
                    TermsOfService = new Uri("https://www.google.com"),
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Danilo Alves",
                        Url = new Uri("https://www.google.com")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Example License",
                        Url = new Uri("https://www.google.com")
                    }
                });

                // using System.Reflection;
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                s.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });
var app = builder.Build();

//https://localhost:7276/swagger/index.html
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI(s =>
        {
            s.RoutePrefix = "";
            s.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi");
        });
//}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
