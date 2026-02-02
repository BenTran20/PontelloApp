using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

namespace PontelloApp.Controllers
{
    public class ProductVariantController : Controller
    {
        private readonly PontelloAppContext _context;

        public ProductVariantController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: ProductVariant
        public async Task<IActionResult> Index()
        {
            var pontelloAppContext = _context.ProductVariants.Include(p => p.Product);
            return View(await pontelloAppContext.ToListAsync());
        }

        // GET: ProductVariant/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productVariant = await _context.ProductVariants
                .Include(p => p.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productVariant == null)
            {
                return NotFound();
            }

            return View(productVariant);
        }

        // GET: ProductVariant/Create
        public IActionResult Create()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "ID", "ProductName");
            return View();
        }

        // POST: ProductVariant/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UnitPrice,StockQuantity,SKU_ExternalID,ProductId")] ProductVariant productVariant)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(productVariant);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "ID", "ProductName", productVariant.ProductId);
            return View(productVariant);
        }

        // GET: ProductVariant/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productVariant = await _context.ProductVariants.FindAsync(id);
            if (productVariant == null)
            {
                return NotFound();
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "ID", "ProductName", productVariant.ProductId);
            return View(productVariant);
        }

        // POST: ProductVariant/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UnitPrice,StockQuantity,SKU_ExternalID,ProductId")] ProductVariant productVariant)
        {
            if (id != productVariant.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductVariantExists(productVariant.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "ID", "ProductName", productVariant.ProductId);
            return View(productVariant);
        }

        // GET: ProductVariant/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productVariant = await _context.ProductVariants
                .Include(p => p.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productVariant == null)
            {
                return NotFound();
            }

            return View(productVariant);
        }

        // POST: ProductVariant/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productVariant = await _context.ProductVariants.FindAsync(id);
            try
            {
                if (productVariant != null)
                {
                    _context.ProductVariants.Remove(productVariant);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");

            }
            return View(productVariant);

        }

        private bool ProductVariantExists(int id)
        {
            return _context.ProductVariants.Any(e => e.Id == id);
        }
    }
}
