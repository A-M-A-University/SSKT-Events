using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SSKT_Events.Models;

namespace SSKT_Events.Controllers;

public class HomeController : Controller
{
    public IActionResult Indexs()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
