using Microsoft.AspNetCore.Mvc;
namespace AwsDocumentPortal.Controllers
{
    public sealed class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Error() => View();
    }
}