using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace ConsignadoLeads.Api.Features.Documents;

public class DocumentsHandler(MongoContext mongo)
{
    /// <summary>
    /// Lightweight existence check used by the upload endpoint so a missing lead returns 404
    /// before any file validation runs (README seção 5: upload never creates a lead).
    /// </summary>
    public async Task EnsureLeadExistsAsync(string leadId)
    {
        var leadExists = await mongo.Leads.Find(l => l.Id == leadId).AnyAsync();
        if (!leadExists)
        {
            throw new LeadNotFoundException(leadId);
        }
    }

    /// <summary>
    /// Write order per design.md (no cross-collection transaction, standalone Mongo): 1)
    /// GridFS upload succeeds first, 2) metadata insert, 3) previous same-type doc marked
    /// replaced. A crash between 1-2 only leaves an unreferenced GridFS blob — never a
    /// metadata row pointing at a missing file.
    /// </summary>
    public async Task<DocumentDto> UploadAsync(string leadId, IFormFile file, string type, string? personalDocumentSubtype)
    {
        var leadExists = await mongo.Leads.Find(l => l.Id == leadId).AnyAsync();
        if (!leadExists)
        {
            throw new LeadNotFoundException(leadId);
        }

        // The "previous same-type document" lookup doesn't depend on the GridFS upload's
        // result (only on leadId/type) — run them concurrently instead of serially.
        var uploadTask = UploadToGridFsAsync(file);
        var previousTask = mongo.GetActiveDocumentsAsync(leadId, type);
        await Task.WhenAll(uploadTask, previousTask);
        var gridFsFileId = await uploadTask;
        var previous = (await previousTask).FirstOrDefault();

        var entity = new LeadDocumentEntity
        {
            Id = Guid.NewGuid().ToString(),
            LeadId = leadId,
            Type = type,
            PersonalDocumentSubtype = personalDocumentSubtype,
            Status = "uploaded",
            Replaces = previous?.Id,
            GridFsFileId = gridFsFileId,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
        };

        await mongo.LeadDocuments.InsertOneAsync(entity);

        if (previous is not null)
        {
            await mongo.LeadDocuments.UpdateOneAsync(
                d => d.Id == previous.Id,
                Builders<LeadDocumentEntity>.Update.Set(d => d.Status, "replaced"));
        }

        return DocumentMapper.ToDto(entity);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListActiveAsync(string leadId)
    {
        var leadExists = await mongo.Leads.Find(l => l.Id == leadId).AnyAsync();
        if (!leadExists)
        {
            throw new LeadNotFoundException(leadId);
        }

        var documents = await mongo.GetActiveDocumentsAsync(leadId);
        return documents.Select(DocumentMapper.ToDto).ToList();
    }

    public async Task DeleteAsync(string leadId, string documentId)
    {
        var filter = Builders<LeadDocumentEntity>.Filter.And(
            Builders<LeadDocumentEntity>.Filter.Eq(d => d.Id, documentId),
            Builders<LeadDocumentEntity>.Filter.Eq(d => d.LeadId, leadId));
        var update = Builders<LeadDocumentEntity>.Update.Set(d => d.Status, "deleted");

        var result = await mongo.LeadDocuments.UpdateOneAsync(filter, update);
        if (result.MatchedCount > 0)
        {
            return;
        }

        var leadExists = await mongo.Leads.Find(l => l.Id == leadId).AnyAsync();
        if (!leadExists)
        {
            throw new LeadNotFoundException(leadId);
        }

        throw new DocumentNotFoundException(documentId);
    }

    private async Task<ObjectId> UploadToGridFsAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        return await mongo.Files.UploadFromStreamAsync(
            file.FileName,
            stream,
            new GridFSUploadOptions { Metadata = new BsonDocument { { "contentType", file.ContentType } } });
    }
}
