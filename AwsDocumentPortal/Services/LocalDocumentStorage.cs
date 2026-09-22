using System.Collections.Concurrent;
using AwsDocumentPortal.Models;

namespace AwsDocumentPortal.Services;

public sealed class LocalDocumentStorage(
    IWebHostEnvironment env,
    IConfiguration config) : ILocalDocumentStorage
{
    private readonly ConcurrentDictionary<Guid, DocumentItem> _documents = new();
    private readonly string _root = Path.Combine(env.ContentRootPath, "App_Data", "uploads");
    private readonly long _maxFileSize = (config.GetValue<long?>("DocumentStorage:MaxFileSizeMb") ?? 10) * 1024L * 1024L;
    private readonly HashSet<string> _allowedExtensions =
        (config.GetSection("DocumentStorage:AllowedExtensions").Get<string[]>() ?? [".pdf", ".png", ".jpg", ".jpeg"])
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<DocumentItem> SaveAsync(
        IFormFile file,
        string documentType,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("File is empty.");

        if (file.Length > _maxFileSize)
            throw new InvalidOperationException("File exceeds the 10 MB limit.");

        var extension = Path.GetExtension(file.FileName);

        if (!_allowedExtensions.Contains(extension))
            throw new InvalidOperationException("File type is not allowed.");

        Directory.CreateDirectory(_root);

        var id = Guid.NewGuid();
        var storageKey = $"{id:N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(_root, storageKey);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream, cancellationToken);

        var item = new DocumentItem
        {
            Id = id,
            FileName = Path.GetFileName(file.FileName),
            DocumentType = documentType,
            SizeBytes = file.Length,
            StorageKey = storageKey
        };

        _documents[id] = item;

        return item;
    }

    public IReadOnlyCollection<DocumentItem> GetAll() =>
        _documents.Values
            .OrderByDescending(x => x.UploadedAtUtc)
            .ToArray();
}