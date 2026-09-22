using System.ComponentModel.DataAnnotations;
namespace AwsDocumentPortal.Models
{
    public sealed class DocumentUploadViewModel
    {
        [Required, Display(Name = "Document type")]
        public string DocumentType { get; set; } = "Identity Proof";
        [Required, Display(Name = "Document")]
        public IFormFile? File { get; set; }
    }
}