using AwsDocumentPortal.Models;
using AwsDocumentPortal.Services;
using Microsoft.AspNetCore.Mvc;
namespace AwsDocumentPortal.Controllers;
public sealed class DocumentsController(IDocumentStorage storage):Controller{
[HttpGet]public IActionResult Index()=>View(storage.GetAll());
[HttpGet]public IActionResult Upload()=>View(new DocumentUploadViewModel());
[HttpPost][ValidateAntiForgeryToken]public async Task<IActionResult>Upload(DocumentUploadViewModel model,CancellationToken ct){if(model.File is null)ModelState.AddModelError(nameof(model.File),"Please select a document.");if(!ModelState.IsValid)return View(model);try{await storage.SaveAsync(model.File!,model.DocumentType,ct);TempData["Success"]="Document uploaded successfully.";return RedirectToAction(nameof(Index));}catch(InvalidOperationException ex){ModelState.AddModelError(nameof(model.File),ex.Message);return View(model);}}}