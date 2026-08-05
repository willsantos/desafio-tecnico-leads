var builder = WebApplication.CreateBuilder(args);

// TODO: registre aqui seus serviços (cliente MongoDB.Driver, repositórios, casos de uso, validação, logging estruturado...).
// A connection string do MongoDB chega via ConnectionStrings__MongoDb (ver docker-compose.yml).

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

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
