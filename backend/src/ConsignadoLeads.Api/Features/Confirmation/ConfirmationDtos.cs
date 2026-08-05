using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Features.Confirmation;

public record ConfirmRequest(string? MockOutcome);

public record ConfirmationResult(int StatusCode, LeadDto Dto);
