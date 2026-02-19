using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

namespace PontelloApp.Controllers
{
    public class VendorProductsController : Controller
    {
        private readonly PontelloAppContext _context;

        public VendorProductsController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: VendorProducts
        public async Task<IActionResult> Index(int? vendorId)
        {
            if (vendorId == null)
                return NotFound();

            // Master
            var vendor = await _context.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VendorID == vendorId);

            if (vendor == null)
                return NotFound();

            ViewBag.Vendor = vendor;

            // Details
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => p.VendorID == vendorId)
                .OrderBy(p => p.ProductName)
                .AsNoTracking()
                .ToListAsync();

            return View(products);
        }



        // GET: VendorProducts/Details/5
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

        // GET: VendorProducts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VendorProducts/Create
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

        // GET: VendorProducts/Edit/5
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

        // POST: VendorProducts/Edit/5
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

        // GET: VendorProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: VendorProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor != null)
            {
                _context.Vendors.Remove(vendor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VendorExists(int id)
        {
            return _context.Vendors.Any(e => e.VendorID == id);
        }
    }
}
