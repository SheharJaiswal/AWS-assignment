using System.Collections.Concurrent;
using AwsDocumentPortal.Models;
namespace AwsDocumentPortal.Services;
public sealed class LocalDocumentStorage(IWebHostEnvironment env,IConfiguration config):IDocumentStorage{
readonly ConcurrentDictionary<Guid,DocumentItem> docs=new();readonly string root=Path.Combine(env.ContentRootPath,"App_Data","uploads");
readonly long max=(config.GetValue<long?>("DocumentStorage:MaxFileSizeMb")??10)*1024L*1024L;
readonly HashSet<string> extensions=(config.GetSection("DocumentStorage:AllowedExtensions").Get<string[]>()??[".pdf",".png",".jpg",".jpeg"]).ToHashSet(StringComparer.OrdinalIgnoreCase);
public async Task<DocumentItem>SaveAsync(IFormFile file,string type,CancellationToken ct){if(file.Length<=0)throw new InvalidOperationException("File is empty.");if(file.Length>max)throw new InvalidOperationException("File exceeds the 10 MB limit.");var ext=Path.GetExtension(file.FileName);if(!extensions.Contains(ext))throw new InvalidOperationException("File type is not allowed.");Directory.CreateDirectory(root);var id=Guid.NewGuid();var key=$"{id:N}{ext.ToLowerInvariant()}";await using var s=File.Create(Path.Combine(root,key));await file.CopyToAsync(s,ct);var item=new DocumentItem{Id=id,FileName=Path.GetFileName(file.FileName),DocumentType=type,SizeBytes=file.Length,StorageKey=key};docs[id]=item;return item;}
public IReadOnlyCollection<DocumentItem>GetAll()=>docs.Values.OrderByDescending(x=>x.UploadedAtUtc).ToArray();}