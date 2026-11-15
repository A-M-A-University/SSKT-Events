
using Microsoft.AspNetCore.Mvc;

namespace SSKT_Events.Controllers
{
    public class CustomerDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
