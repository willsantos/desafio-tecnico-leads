using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Logging;
using ConsignadoLeads.Api.Features.Consultation;
using ConsignadoLeads.Api.Features.Documents;
using ConsignadoLeads.Api.Features.Identification;
using ConsignadoLeads.Api.Features.Leads;
using ConsignadoLeads.Api.Features.ProfessionalBankingData;
using ConsignadoLeads.Api.Features.Simulation;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// TODO: registre aqui seus serviços (repositórios, casos de uso, validação, logging estruturado...).
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

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();
app.UseCors();

await app.Services.GetRequiredService<MongoContext>().EnsureIndexesAsync();

// Obrigatório para a suíte de avaliação: 200 quando a API está pronta.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapConsultationEndpoints();
app.MapLeadsEndpoints();
app.MapSimulationEndpoints();
app.MapIdentificationEndpoints();
app.MapProfessionalBankingDataEndpoints();
app.MapDocumentsEndpoints();

// TODO: implemente o restante do contrato de API descrito no README (seção "CONTRATO DE API"):
//   POST   /leads/{id}/confirm
//   POST   /leads/{id}/retry-submission

app.Run();

// Exposes the top-level Program class publicly so ConsignadoLeads.Api.Tests can spin up
// WebApplicationFactory<Program> for integration tests.
public partial class Program;
