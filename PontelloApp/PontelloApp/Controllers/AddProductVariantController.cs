using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Custom_Controllers;
using PontelloApp.Data;
using PontelloApp.Models;

namespace PontelloApp.Controllers
{
    public class AddProductVariantController : CognizantController
    {
        private readonly PontelloAppContext _context;

        public AddProductVariantController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: AddProductVariant
        public async Task<IActionResult> Index(int? id)
        {
            Product product = await _context.Products
                .Include(p => p.Variants)
                .ThenInclude(v => v.Options)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (product == null) return NotFound();
            ViewBag.Product = product; 

            var variants = _context.ProductVariants
                        .Where(v => v.ProductId == id)
                        .Include(v => v.Options)   
                        .ToListAsync();

            PopulateDropDownLists();
            return View(await variants);
        }


        // GET: AddProductVariant/Add
        public IActionResult Add(int? ProductId)
        {
            if (!ProductId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            ProductVariant v = new ProductVariant
            {
                ProductId = ProductId.GetValueOrDefault(),
                Options = new List<Variant>() 
            };

            var product = _context.Products.Find(ProductId.Value);
            ViewBag.Product = product;
            return View(v);
        }

        // POST: AddProductVariant/Add
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ProductVariant variant)
        {
            try
            {
                if (variant.Options == null)
                    variant.Options = new List<Variant>();

                foreach (var opt in variant.Options)
                {
                    opt.ProductVariant = variant;
                }

                if (ModelState.IsValid)
                {
                    _context.ProductVariants.Add(variant);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { id = variant.ProductId });
                }

            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes.");
            }
            return View(variant);
        }

        // GET: AddProductVariant/Update/5
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.Options)
                .FirstOrDefaultAsync(v => v.Id == id);
            
            if (variant == null)
            {
                return NotFound();
            }

            return View(variant);
        }

        // POST: AddProductVariant/Update/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, ProductVariant variant)
        {
            var variantToUpdate = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.Options)
            .FirstOrDefaultAsync(v => v.Id == id);

            if (variantToUpdate == null) return NotFound();

            if (await TryUpdateModelAsync<ProductVariant>(variantToUpdate, "",
                v => v.SKU_ExternalID,
                v => v.UnitPrice,
                v => v.StockQuantity))
            {
                if (variantToUpdate.Options.Any())
                {
                    _context.Variants.RemoveRange(variantToUpdate.Options);
                }

                if (variant.Options != null && variant.Options.Any())
                {
                    foreach (var opt in variant.Options)
                    {
                        opt.ProductVariantId = variantToUpdate.Id;
                        _context.Variants.Add(opt);
                    }
                }

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { id = variantToUpdate.ProductId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductVariantExists(variantToUpdate.Id))
                        return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes.");
                }
            }
            return View(variantToUpdate);
        }

        // GET: AddProductVariant/Remove/5
        public async Task<IActionResult> Remove(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var variant = await _context.ProductVariants
                            .Include(v => v.Product)
                            .Include(v => v.Options)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null) return NotFound();

            return View(variant);
        }

        // POST: AddProductVariant/Remove/5
        [HttpPost, ActionName("Remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveConfirmed(int id)
        {
            var variant = await _context.ProductVariants
                            .Include(v => v.Product)
                            .Include(v => v.Options)
                            .FirstOrDefaultAsync(v => v.Id == id);

            try
            {
                _context.ProductVariants.Remove(variant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { id = variant.ProductId });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to delete variant.");
            }

            return View(variant);
        }
        private SelectList ProductSelectList(int? selectedId)
        {
            return new SelectList(_context.Products
                .OrderBy(d => d.ProductName), "ID", "ProductName", selectedId);
        }

        private void PopulateDropDownLists(ProductVariant? productVariant = null)
        {
            ViewData["ProductId"] = ProductSelectList(productVariant?.ProductId);
        }

        private bool ProductVariantExists(int id)
        {
            return _context.ProductVariants.Any(e => e.Id == id);
        }
    }
}
