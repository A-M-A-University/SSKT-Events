using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Data;
using SSKT_Events.Models;

namespace SSKT_Events.Controllers;

[Authorize(Roles = "Organizer")]
public class OrganizerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public OrganizerController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Get the currently logged-in Organizer
    private async Task<Organizer?> GetCurrentOrganizerAsync()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return null;

        return await _context.Organizers
            .FirstOrDefaultAsync(o => o.UserId == userId);
    }

    
// ORGANIZER DASHBOARD / OVERVIEW
public async Task<IActionResult> Index()
{
    var organizer = await GetCurrentOrganizerAsync();

    if (organizer == null)
        return NotFound();

    var partnerIds = await _context.OrganizerSuppliers
        .Where(p => p.OrganizerId == organizer.Id)
        .Select(p => p.SupplierId)
        .ToListAsync();

    var packageIds = await _context.EventPackages
        .Where(p => p.OrganizerId == organizer.Id)
        .Select(p => p.Id)
        .ToListAsync();

    ViewBag.Organizer = organizer;

    // Dashboard statistics
    ViewBag.TotalPackages = packageIds.Count;
    ViewBag.TotalSuppliers = partnerIds.Count;

    ViewBag.TotalServices = await _context.SupplierServices
        .CountAsync(s =>
            s.IsActive &&
            partnerIds.Contains(s.SupplierId));

    ViewBag.TotalEvents = await _context.SupplierBookings
        .Where(b =>
            b.PackageId.HasValue &&
            packageIds.Contains(b.PackageId.Value))
        .Select(b => b.EventId)
        .Distinct()
        .CountAsync();

    // Partner suppliers
    ViewBag.Suppliers = await _context.Suppliers
        .Where(s => partnerIds.Contains(s.Id))
        .ToListAsync();

    // Available services
    ViewBag.Services = await _context.SupplierServices
        .Where(s =>
            s.IsActive &&
            partnerIds.Contains(s.SupplierId))
        .ToListAsync();

    return View();
}


    // VIEW PACKAGES
    public async Task<IActionResult> Packages()
    {
        var organizer = await GetCurrentOrganizerAsync();

        if (organizer == null)
            return NotFound();

        ViewBag.Organizer = organizer;

        ViewBag.Services = await _context.SupplierServices
            .Where(s => s.IsActive)
            .ToListAsync();

        var myPackages = await _context.EventPackages
            .Include(p => p.PackageServices)
            .Where(p => p.OrganizerId == organizer.Id)
            .ToListAsync();

        // Populate the temporary ServiceIds property for existing views
        foreach (var package in myPackages)
        {
            package.ServiceIds = package.PackageServices
                .Select(ps => ps.ServiceId)
                .ToList();
        }

        return View(myPackages);
    }

    // PUBLISH / UNPUBLISH PACKAGE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int packageId)
    {
        var organizer = await GetCurrentOrganizerAsync();

        if (organizer == null)
            return NotFound();

        var package = await _context.EventPackages
            .FirstOrDefaultAsync(p =>
                p.Id == packageId &&
                p.OrganizerId == organizer.Id);

        if (package == null)
            return NotFound();

        package.IsPublished = !package.IsPublished;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Packages));
    }

    // CREATE PACKAGE - GET
    [HttpGet]
    public async Task<IActionResult> CreatePackage()
    {
        var organizer = await GetCurrentOrganizerAsync();

        if (organizer == null)
            return NotFound();

        var partnerIds = await _context.OrganizerSuppliers
            .Where(p => p.OrganizerId == organizer.Id)
            .Select(p => p.SupplierId)
            .ToListAsync();

        ViewBag.Organizer = organizer;

        ViewBag.Suppliers = await _context.Suppliers
            .Where(s => partnerIds.Contains(s.Id))
            .ToListAsync();

        ViewBag.Services = await _context.SupplierServices
            .Where(s => s.IsActive &&
                        partnerIds.Contains(s.SupplierId))
            .ToListAsync();

        return View();
    }

    // CREATE PACKAGE - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePackage(
        string name,
        string description,
        List<int>? serviceIds)
    {
        var organizer = await GetCurrentOrganizerAsync();

        if (organizer == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Package name is required.";
            return RedirectToAction(nameof(CreatePackage));
        }

        var selectedIds = serviceIds?.Distinct().ToList()
            ?? new List<int>();

        if (selectedIds.Count == 0)
        {
            TempData["Error"] =
                "Pick at least one service for the package.";

            return RedirectToAction(nameof(CreatePackage));
        }

        // Get this organizer's partner supplier IDs
        var partnerIds = await _context.OrganizerSuppliers
            .Where(p => p.OrganizerId == organizer.Id)
            .Select(p => p.SupplierId)
            .ToListAsync();

        // Only active services from partner suppliers are allowed
        var selectedServices = await _context.SupplierServices
            .Where(s =>
                selectedIds.Contains(s.Id) &&
                s.IsActive &&
                partnerIds.Contains(s.SupplierId))
            .ToListAsync();

        if (selectedServices.Count != selectedIds.Count)
        {
            TempData["Error"] =
                "One or more selected services are invalid or unavailable.";

            return RedirectToAction(nameof(CreatePackage));
        }

        var total = selectedServices.Sum(s => s.Price);

        var package = new EventPackage
        {
            OrganizerId = organizer.Id,
            Name = name.Trim(),
            Description = description ?? "",
            Price = total,
            IsPublished = false,
            PackageServices = selectedServices
                .Select(s => new PackageService
                {
                    ServiceId = s.Id
                })
                .ToList()
        };

        _context.EventPackages.Add(package);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Packages));
    }
}
