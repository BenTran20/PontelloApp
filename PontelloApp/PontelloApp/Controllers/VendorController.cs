using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

namespace PontelloApp.Controllers
{
    public class VendorController : Controller
    {
        private readonly PontelloAppContext _context;

        public VendorController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: Vendor
        public async Task<IActionResult> Index()
        {

            var vendors = await _context.Vendors
                    .Where(v => !v.IsArchived)
                    .OrderBy(v => v.Name)
                    .ToListAsync();

            return View(vendors);

        }

        // GET: Vendor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(m => m.VendorID == id);
            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // GET: Vendor/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Vendor/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VendorID,Name,ContactName,Phone,Email,EIN,IsTaxExempt,IsArchived,RowVersion")] Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vendor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vendor);
        }

        // GET: Vendor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }
            return View(vendor);
        }

        // POST: Vendor/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VendorID,Name,ContactName,Phone,Email,EIN,IsTaxExempt,IsArchived,RowVersion")] Vendor vendor)
        {
            if (id != vendor.VendorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vendor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorExists(vendor.VendorID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vendor);
        }


        // GET: Vendor/Archive/5
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
                return NotFound();

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorID == id);

            if (vendor == null)
                return NotFound();

            return View(vendor);
        }


        // POST: Vendor/Archive/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
                return NotFound();

            vendor.IsArchived = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Vendor/Archived
        public async Task<IActionResult> Archived()
        {
            var archivedVendors = await _context.Vendors
                .Where(v => v.IsArchived)
                .OrderBy(v => v.Name)
                .AsNoTracking()
                .ToListAsync();

            return View(archivedVendors);
        }

        private bool VendorExists(int id)
        {
            return _context.Vendors.Any(e => e.VendorID == id);
        }
    }
}
