using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Exceptions;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// TODO: registre aqui seus serviços (repositórios, casos de uso, validação, logging estruturado...).
// A connection string do MongoDB chega via ConnectionStrings__MongoDb (ver docker-compose.yml).

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("ConnectionStrings__MongoDb não configurada.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => new MongoContext(sp.GetRequiredService<IMongoClient>(), mongoConnectionString));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

await app.Services.GetRequiredService<MongoContext>().EnsureIndexesAsync();

// Obrigatório para a suíte de avaliação: 200 quando a API está pronta.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// TODO: implemente o contrato de API descrito no README (seção "CONTRATO DE API"):
//   POST   /leads/consultation
//   PUT    /leads/{id}/steps/consultation
//   POST   /leads/{id}/steps/simulation
//   PATCH  /leads/{id}/steps/simulation/{simulationId}/select
//   PUT    /leads/{id}/steps/identification
//   PUT    /leads/{id}/steps/professional-banking-data
//   POST   /leads/{id}/documents
//   GET    /leads/{id}/documents
//   DELETE /leads/{id}/documents/{documentId}
//   POST   /leads/{id}/confirm
//   POST   /leads/{id}/retry-submission
//   GET    /leads
//   GET    /leads/{id}

app.Run();
