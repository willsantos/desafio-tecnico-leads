using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Features.Leads;

public class LeadsHandler(MongoContext mongo)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedLeadsResponse> ListAsync(LeadsQuery query)
    {
        var page = query.Page is > 0 ? query.Page.Value : 1;
        var pageSize = query.PageSize switch
        {
            null or <= 0 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize.Value,
        };

        var filterBuilder = Builders<Lead>.Filter;
        var filters = new List<FilterDefinition<Lead>>();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statuses = query.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            filters.Add(filterBuilder.In(l => l.Status, statuses));
        }

        if (!string.IsNullOrWhiteSpace(query.CurrentStep))
        {
            filters.Add(filterBuilder.Eq(l => l.Progress.CurrentStep, query.CurrentStep));
        }

        if (!string.IsNullOrWhiteSpace(query.Cpf))
        {
            filters.Add(filterBuilder.Eq("consultation.input.cpf", query.Cpf));
        }

        var filter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;

        var totalItems = await mongo.Leads.CountDocumentsAsync(filter);
        var leads = await mongo.Leads
            .Find(filter)
            .SortByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedLeadsResponse(leads.Select(LeadSummaryMapper.ToDto).ToList(), page, pageSize, totalItems, totalPages);
    }

    public async Task<LeadDto> GetByIdAsync(string id)
    {
        var lead = await mongo.Leads.Find(l => l.Id == id).FirstOrDefaultAsync()
            ?? throw new LeadNotFoundException(id);

        var activeDocuments = await mongo.LeadDocuments
            .Find(d => d.LeadId == id && d.Status != "deleted" && d.Status != "replaced")
            .ToListAsync();

        return LeadMapper.ToDto(lead, activeDocuments.Select(DocumentMapper.ToDto).ToList());
    }
}
