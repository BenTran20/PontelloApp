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
    public class ProductController : ElephantController
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
                .Include(p => p.Vendor)
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

            int totalItems = await products.CountAsync();
            ViewData["TotalItems"] = totalItems;

            var pagedData = await PaginatedList<Product>.CreateAsync(products.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Vendor)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Options)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (product == null) return NotFound();

            LoadCategoryParents(product.Category);

            return View(product);
        }


        // GET: Products/Create
        public IActionResult Create()
        {
            var product = new Product
            {
                IsActive = true 
            };

            PopulateDropDownLists();
            return View(product);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,Handle,VendorID,Type,Tag,Description,IsActive,CategoryID")] Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    var returnUrl = ViewData["returnURL"]?.ToString();
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    TempData["Success"] = "Create new product successfully ";
                    if (product.IsActive == true)
                    {
                        TempData["Status"] = "Status: Active";
                    }
                    else
                    {
                        TempData["Status"] = "Status: Archived";

                    }
                    return Redirect(returnUrl);
                }
            }
            catch (DbUpdateException dex)
            {
                if (dex.InnerException != null && dex.InnerException.Message.Contains("UNIQUE"))
                {
                    ModelState.AddModelError("", "This product already exists. Please choose a different Handle.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to create product. Try again, and if the problem persists see your system administrator.");
                }
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
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            var productToUpdate = await _context.Products.FirstOrDefaultAsync(p => p.ID == id);
            if (productToUpdate == null) return NotFound();

            _context.Entry(productToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            if (await TryUpdateModelAsync<Product>(productToUpdate, "",
                p => p.ProductName, p => p.Description, p => p.IsActive, p => p.CategoryID,
                p => p.Handle, p => p.VendorID, p => p.Type, p => p.Tag))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    var returnUrl = ViewData["returnURL"]?.ToString();
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    TempData["Success"] = "Edit product successfully";
                    if (productToUpdate.IsActive == true)
                    {
                        TempData["Status"] = "Status: Active";
                    }
                    else
                    {
                        TempData["Status"] = "Status: Archived";

                    }
                    return Redirect(returnUrl);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Product)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to save changes. The Product was archived by another user.");
                    }
                    else
                    {
                        var databaseValues = (Product)databaseEntry.ToObject();
                        if (databaseValues.ProductName != clientValues.ProductName)
                            ModelState.AddModelError("ProductName", "Current value: "
                                + databaseValues.ProductName);
                        if (databaseValues.Handle != clientValues.Handle)
                            ModelState.AddModelError("Handle", "Current value: "
                                + databaseValues.Handle);
                        if (databaseValues.Vendor != clientValues.Vendor)
                            ModelState.AddModelError("Vendor", "Current value: "
                                + databaseValues.Vendor);
                        if (databaseValues.Type != clientValues.Type)
                            ModelState.AddModelError("Type", "Current value: "
                                + databaseValues.Type);
                        if (databaseValues.Tag != clientValues.Tag)
                            ModelState.AddModelError("Tag", "Current value: "
                                + databaseValues.Tag);
                        if (databaseValues.Description != clientValues.Description)
                            ModelState.AddModelError("Description", "Current value: "
                                + databaseValues.Description);
                        if (databaseValues.IsActive != clientValues.IsActive)
                            ModelState.AddModelError("IsActive", "Current value: "
                                + databaseValues.IsActive);
                        //For the foreign key, we need to go to the database to get the information to show
                        if (databaseValues.CategoryID != clientValues.CategoryID)
                        {
                            Category? databaseCategory = await _context.Categories.FirstOrDefaultAsync(i => i.ID == databaseValues.CategoryID);
                            ModelState.AddModelError("CategoryID", $"Current value: {databaseCategory?.Name}");
                        }
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                                + "was modified by another user after you received your values. The "
                                + "edit operation was canceled and the current values in the database "
                                + "have been displayed. If you still want to save your version of this record, click "
                                + "the Save button again. Otherwise click the 'Back to Product List' hyperlink.");

                        //Final steps before redisplaying: Update RowVersion from the Database
                        //and remove the RowVersion error from the ModelState
                        productToUpdate.RowVersion = databaseValues.RowVersion ?? Array.Empty<byte>();
                        ModelState.Remove("RowVersion");
                    }
                }
                catch (DbUpdateException dex)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
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
                .Include(p => p.Variants)
                .ThenInclude(p => p.Options)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (product == null) return NotFound();
            LoadCategoryParents(product?.Category);

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, Byte[] RowVersion)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .ThenInclude(p => p.Options)
                .FirstOrDefaultAsync(p => p.ID == id);

            try
            {
                if (product != null)
                {
                    _context.Entry(product).Property("RowVersion").OriginalValue = RowVersion;
                    LoadCategoryParents(product?.Category);
                    product.IsActive = false;
                }

                await _context.SaveChangesAsync();
                var returnUrl = ViewData["returnURL"]?.ToString();

                if (string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToAction(nameof(Index));
                }
                TempData["Success"] = "Archive product Successfully";
                TempData["Status"] = "Status: Archived";
                return Redirect(returnUrl);

            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "The Product you attempted to archive "
                                + "was modified by another user. Please go back on refresh.");
                ViewData["CantSave"] = "disabled='disabled'";
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to archive Product. Try again, and if the problem persists see your system administrator.");
            }

            return View(product);
        }
        private SelectList CategorySelectList(int? selectedId)
        {
            return new SelectList(_context.Categories
                .OrderBy(d => d.Name), "ID", "Name", selectedId);
        }
        private SelectList VendorSelectList(int? selectedId)
        {
            return new SelectList(_context.Vendors
                .OrderBy(d => d.Name), "VendorID", "Name", selectedId);
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

            ViewData["VendorID"] = VendorSelectList(product?.VendorID);

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

        private void LoadCategoryParents(Category? category)
        {
            while (category != null && category.ParentCategoryID != null)
            {
                category.ParentCategory = _context.Categories
                    .FirstOrDefault(c => c.ID == category.ParentCategoryID);
                category = category.ParentCategory;
            }
        }

    }
}
