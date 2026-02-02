using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Custom_Controllers;
using PontelloApp.Data;
using PontelloApp.Models;
using PontelloApp.Utilities;
using System.Numerics;

namespace PontelloApp.Controllers
{
    public class ProductController : CognizantController
    {
        private readonly PontelloAppContext _context;

        public ProductController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string? SearchString, int? CategoryID,
                     int? page, int? pageSizeID, string? actionButton, string sortDirection = "asc", string sortField = "Product")
        {
            string[] sortOptions = new[] { "None", "A-Z", "Z-A" };

            ViewData["Filtering"] = "btn-outline-secondary";
            int numberFilters = 0;

            PopulateDropDownLists();

            var products = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .AsNoTracking();

            if (!String.IsNullOrEmpty(SearchString))
            {
                products = products.Where(p => p.ProductName.ToUpper().Contains(SearchString.ToUpper()));

            }
            if (CategoryID.HasValue)
            {
                products = products.Where(p => p.CategoryID == CategoryID);
                numberFilters++;

            }
            //Add if include price range filter
            //if (MaxPrice.HasValue)
            //{
            //    products = products.Where(p => p.UnitPrice <= MaxPrice);
            //    numberFilters++;

            //}
            //if (MinPrice.HasValue)
            //{
            //    products = products.Where(p => p.UnitPrice >= MinPrice);

            //}

            if (numberFilters != 0)
            {
                ViewData["numberFilters"] = "(" + numberFilters.ToString() + ")";

                @ViewData["ShowFilter"] = "show";
            }

            if (!String.IsNullOrEmpty(actionButton))
            {
                page = 1;

                if (sortOptions.Contains(actionButton))
                {
                    if (actionButton == sortField)
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;
                }
            }

            if (sortField == "A-Z")
            {
                if (sortDirection == "asc")
                {
                    products = products
                        .OrderBy(p => p.ProductName.ToUpper());
                }
            }
            else if (sortField == "Z-A")
            {
                if (sortDirection == "asc")
                {
                    products = products
                        .OrderByDescending(p => p.ProductName.ToUpper());
                }
            }
            else
            {
                if (sortDirection == "asc")
                {
                    products = products
                        .OrderBy(p => p.ProductName.ToUpper());
                }
                else
                {
                    products = products
                        .OrderByDescending(p => p.ProductName.ToUpper());
                }
            }

            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Product>.CreateAsync(products.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .ThenInclude(v => v.Options)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            PopulateDropDownLists();
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,Description,IsActive,CategoryID")] Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException dex)
            {
                throw new Exception(dex.Message);
            }

            PopulateDropDownLists(product);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            PopulateDropDownLists(product);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var productToUpdate = await _context.Products.FirstOrDefaultAsync(p => p.ID == id);
            if (productToUpdate == null) return NotFound();

            if (await TryUpdateModelAsync<Product>(productToUpdate, "",
                p => p.ProductName, p => p.Description, p => p.IsActive, p => p.CategoryID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ID == id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException dex)
                {
                    throw new Exception(dex.Message);
                }
            }
            PopulateDropDownLists(productToUpdate);
            return View(productToUpdate);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        private SelectList CategorySelectList(int? selectedId)
        {
            return new SelectList(_context.Categories
                .OrderBy(d => d.Name), "ID", "Name", selectedId);
        }

        private void PopulateDropDownLists(Product? product = null)
        {
            var rootCategories = _context.Categories
                .Where(c => c.ParentCategoryID == null)
                .Include(c => c.SubCategories)                  
                    .ThenInclude(sc1 => sc1.SubCategories)      
                        .ThenInclude(sc2 => sc2.SubCategories)  
                            .ThenInclude(sc3 => sc3.SubCategories) 
                                .ThenInclude(sc4 => sc4.SubCategories) 
                                    .ThenInclude(sc5 => sc5.SubCategories)
                                        .ThenInclude(sc6 => sc6.SubCategories) 
                .ToList();

            ViewData["CategoryID"] =
                BuildCategorySelectList(rootCategories, product?.CategoryID);
        }

        private List<SelectListItem> BuildCategorySelectList(IEnumerable<Category> categories,
            int? selectedId, int level = 0)
        {
            var items = new List<SelectListItem>();

            foreach (var category in categories)
            {
                items.Add(new SelectListItem
                {
                    Value = category.ID.ToString(),
                    Text = $"{new string('-', level * 2)} {category.Name}", 
                    Selected = category.ID == selectedId
                });

                if (category.SubCategories.Any())
                {
                    items.AddRange(
                        BuildCategorySelectList(category.SubCategories, selectedId, level + 1)
                    );
                }
            }

            return items;
        }

        public async Task<IActionResult> Archive()
        {
            var archivedProducts = await _context.Products
                .Where(p => !p.IsActive)
                .Include(p => p.Category)
                .AsNoTracking()
                .ToListAsync();

            return View(archivedProducts);
        }


    }
}
