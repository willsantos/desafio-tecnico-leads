using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Logging;
using ConsignadoLeads.Api.Features.Confirmation;
using ConsignadoLeads.Api.Features.Consultation;
using ConsignadoLeads.Api.Features.Documents;
using ConsignadoLeads.Api.Features.Identification;
using ConsignadoLeads.Api.Features.Leads;
using ConsignadoLeads.Api.Features.ProfessionalBankingData;
using ConsignadoLeads.Api.Features.Simulation;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// A connection string do MongoDB chega via ConnectionStrings__MongoDb (ver docker-compose.yml).
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("ConnectionStrings__MongoDb não configurada.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => new MongoContext(sp.GetRequiredService<IMongoClient>(), mongoConnectionString));
builder.Services.AddSingleton<MockOutcomeResolver>();

builder.Services.AddScoped<ConsultationHandler>();
builder.Services.AddScoped<LeadsHandler>();
builder.Services.AddScoped<SimulationHandler>();
builder.Services.AddScoped<IdentificationHandler>();
builder.Services.AddScoped<ProfessionalBankingDataHandler>();
builder.Services.AddScoped<DocumentsHandler>();
builder.Services.AddSingleton<IMainSystemMock, MainSystemMock>();
builder.Services.AddScoped<ConfirmationHandler>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ConsignadoLeads API", Version = "v1" }));

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.GetRequiredService<MongoContext>().EnsureIndexesAsync();

// Obrigatório para a suíte de avaliação: 200 quando a API está pronta.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapConsultationEndpoints();
app.MapLeadsEndpoints();
app.MapSimulationEndpoints();
app.MapIdentificationEndpoints();
app.MapProfessionalBankingDataEndpoints();
app.MapDocumentsEndpoints();
app.MapConfirmationEndpoints();

app.Run();

// Exposes the top-level Program class publicly so ConsignadoLeads.Api.Tests can spin up
// WebApplicationFactory<Program> for integration tests.
public partial class Program;
