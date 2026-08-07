using ConsignadoLeads.Api.Core.Exceptions;

namespace ConsignadoLeads.Api.Tests.Unit.Core;

/// <summary>
/// Exercising the per-entity id accessor and message format of the 404 exceptions. The integration
/// suite throws them but never reads the convenience properties (LeadId/DocumentId/SimulationId),
/// which is the uncovered half of each class.
/// </summary>
[Trait("Category", "Unit")]
public class NotFoundExceptionTests
{
    [Fact]
    public void LeadNotFoundException_ExposesLeadIdAndMessage()
    {
        var ex = new LeadNotFoundException("lead-123");
        Assert.Equal("lead-123", ex.LeadId);
        Assert.Contains("lead-123", ex.Message);
        Assert.Contains("não encontrado", ex.Message);
    }

    [Fact]
    public void DocumentNotFoundException_ExposesDocumentIdAndMessage()
    {
        var ex = new DocumentNotFoundException("doc-456");
        Assert.Equal("doc-456", ex.DocumentId);
        Assert.Contains("doc-456", ex.Message);
        Assert.Contains("não encontrado", ex.Message);
    }

    [Fact]
    public void SimulationNotFoundException_ExposesSimulationIdAndMessage()
    {
        var ex = new SimulationNotFoundException("sim-789");
        Assert.Equal("sim-789", ex.SimulationId);
        Assert.Contains("sim-789", ex.Message);
        Assert.Contains("não encontrada", ex.Message);
    }
}
