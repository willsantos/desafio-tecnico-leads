using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace ConsignadoLeads.Api.Core;

/// <summary>
/// Single place that knows the "leads"/"lead_documents" collection names and exposes
/// typed accessors + the GridFS bucket used for document uploads.
/// </summary>
public class MongoContext
{
    static MongoContext()
    {
        var camelCaseConventions = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("camelCase", camelCaseConventions, _ => true);
    }

    public MongoContext(IMongoClient client, string connectionString)
    {
        var databaseName = MongoUrl.Create(connectionString).DatabaseName
            ?? throw new InvalidOperationException("ConnectionStrings__MongoDb must include a database name.");

        var database = client.GetDatabase(databaseName);

        Leads = database.GetCollection<Lead>("leads");
        LeadDocuments = database.GetCollection<LeadDocumentEntity>("lead_documents");
        Files = new GridFSBucket(database);
    }

    public IMongoCollection<Lead> Leads { get; }

    public IMongoCollection<LeadDocumentEntity> LeadDocuments { get; }

    public GridFSBucket Files { get; }

    /// <summary>
    /// Creates the collection indexes if they don't already exist. Safe to call on every
    /// startup — MongoDB's createIndex is a no-op when an identical index already exists.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var leadIndexes = new List<CreateIndexModel<Lead>>
        {
            new(Builders<Lead>.IndexKeys.Ascending(lead => lead.Status).Ascending("progress.currentStep")),
            new(Builders<Lead>.IndexKeys.Ascending("consultation.input.cpf")),
            new(Builders<Lead>.IndexKeys.Descending(lead => lead.CreatedAt)),
        };
        await Leads.Indexes.CreateManyAsync(leadIndexes, cancellationToken);

        var leadDocumentIndexes = new List<CreateIndexModel<LeadDocumentEntity>>
        {
            new(Builders<LeadDocumentEntity>.IndexKeys.Ascending(doc => doc.LeadId).Ascending(doc => doc.Status)),
        };
        await LeadDocuments.Indexes.CreateManyAsync(leadDocumentIndexes, cancellationToken);
    }

    /// <summary>Fetches a lead by id or throws <see cref="LeadNotFoundException"/>. Shared by every
    /// slice that needs the full lead document (as opposed to a lightweight existence check).</summary>
    public async Task<Lead> GetLeadOrThrowAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Leads.Find(l => l.Id == id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new LeadNotFoundException(id);
    }

    /// <summary>Active (non-deleted, non-replaced) documents for a lead, optionally narrowed to one
    /// <paramref name="type"/>. Shared by Confirmation, Documents and Leads — the "active document"
    /// predicate is defined once here instead of re-derived per slice.</summary>
    public async Task<List<LeadDocumentEntity>> GetActiveDocumentsAsync(string leadId, string? type = null, CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<LeadDocumentEntity>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(d => d.LeadId, leadId),
            filterBuilder.Ne(d => d.Status, "deleted"),
            filterBuilder.Ne(d => d.Status, "replaced"));
        if (type is not null)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(d => d.Type, type));
        }

        return await LeadDocuments.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Shared optimistic-concurrency update (AD-004): builds the id(+version) filter, applies
    /// <paramref name="update"/> atomically via <c>findOneAndUpdate</c>, and disambiguates a miss
    /// into <see cref="LeadNotFoundException"/> (id doesn't exist) vs <see cref="VersionConflictException"/>
    /// (id exists, version diverged) — used by every <c>PUT /steps/*</c> handler that accepts an
    /// optional <c>expectedVersion</c>.
    /// </summary>
    public async Task<Lead> UpdateWithVersionCheckAsync(string id, int? expectedVersion, UpdateDefinition<Lead> update, CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Lead>.Filter;
        var filter = expectedVersion is int version
            ? filterBuilder.And(filterBuilder.Eq(l => l.Id, id), filterBuilder.Eq(l => l.Version, version))
            : filterBuilder.Eq(l => l.Id, id);

        var options = new FindOneAndUpdateOptions<Lead> { ReturnDocument = ReturnDocument.After };
        var updated = await Leads.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (updated is not null)
        {
            return updated;
        }

        var existing = await Leads.Find(l => l.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            throw new LeadNotFoundException(id);
        }

        throw new VersionConflictException(expectedVersion!.Value, existing.Version);
    }
}
