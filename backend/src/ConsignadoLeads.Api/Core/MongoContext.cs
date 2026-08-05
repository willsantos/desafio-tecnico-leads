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
}
