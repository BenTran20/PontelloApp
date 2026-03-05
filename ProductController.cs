using System.Drawing;
using System.Numerics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PontelloApp.Custom_Controllers;
using PontelloApp.Data;
using PontelloApp.Models;
using PontelloApp.Ultilities;
using PontelloApp.Utilities;

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

        [HttpGet]
        public JsonResult GetVendor(int? id)
        {
            return Json(VendorSelectList(id));
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

        public IActionResult DownloadPontello()
        {
            var products = _context.Variants
                .Include(p => p.ProductVariant)
                .ThenInclude(p => p.Product)
                .ThenInclude(p => p.Category)
                .OrderByDescending(a => a.ProductVariant.Product.ProductName)
                .Select(a => new
                {
                    Product = a.ProductVariant.Product.ProductName,
                    Handle = a.ProductVariant.Product.Handle,
                    Vendor = a.ProductVariant.Product.Vendor.Name,
                    Types = a.ProductVariant.Product.Type,
                    Tags = a.ProductVariant.Product.Tag,
                    Description = a.ProductVariant.Product.Description,
                    Status = a.ProductVariant.Product.IsActive,
                    Category = a.ProductVariant.Product.Category.Name,
                    UnitPrice = a.ProductVariant.UnitPrice,
                    Stock = a.ProductVariant.StockQuantity,
                    SKU = a.ProductVariant.SKU_ExternalID,
                    Weight = a.ProductVariant.Weight,
                    Unit = a.ProductVariant.Unit,
                    Code = a.ProductVariant.Barcode,
                    Policy = a.ProductVariant.InventoryPolicy,
                    VariantStatus = a.ProductVariant.Status,
                    VariantName = a.Name,
                    VariantValue = a.Value
                })
                .ToList();

            if (!products.Any())
                return NotFound("No data.");

            var sb = new StringBuilder();

            sb.AppendLine("Product,Handle,Vendor,Types,Tags,Description,Status,Category,UnitPrice,Stock,SKU,Weight,Unit,Code,Policy,VariantStatus,VariantName,VariantValue");

            foreach (var p in products)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    CsvEscape(p.Product),
                    CsvEscape(p.Handle),
                    CsvEscape(p.Vendor),
                    CsvEscape(p.Types),
                    CsvEscape(p.Tags),
                    CsvEscape(p.Description),
                    CsvEscape(p.Status.ToString()),
                    CsvEscape(p.Category),
                    CsvEscape(p.UnitPrice.ToString()),
                    CsvEscape(p.Stock.ToString()),
                    CsvEscape(p.SKU),
                    CsvEscape(p.Weight.ToString()),
                    CsvEscape(p.Unit.ToString()),
                    CsvEscape(p.Code),
                    CsvEscape(p.Policy.ToString()),
                    CsvEscape(p.VariantStatus.ToString()),
                    CsvEscape(p.VariantName),
                    CsvEscape(p.VariantValue)
                }));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "PontelloSports.csv");
        }

        private string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }

        [HttpPost]
        public async Task<IActionResult> InsertFromCsv(IFormFile theExcel)
        {
            string feedBack = string.Empty;
            if (theExcel != null)
            {
                string mimeType = theExcel.ContentType;
                long fileLength = theExcel.Length;
                if (!(mimeType == "" || fileLength == 0))
                {
                    if (mimeType.Contains("csv") || theExcel.FileName.EndsWith(".csv"))
                    {
                        using (var excel = new ExcelPackage())
                        {
                            var workSheet = excel.Workbook.Worksheets.Add("TempSheet");

                            using (var reader = new StreamReader(theExcel.OpenReadStream()))
                            {
                                string csvText = await reader.ReadToEndAsync();

                                var format = new ExcelTextFormat
                                {
                                    Delimiter = ',',
                                    TextQualifier = '"'
                                };

                                workSheet.Cells.LoadFromText(csvText, format);
                            }

                            #region Error Handling
                            var start = workSheet.Dimension.Start;
                            var end = workSheet.Dimension.End;
                            int successCount = 0;
                            int errorCount = 0;

                            if (workSheet.Cells[1, 1].Text == "ProductName" || workSheet.Cells[1, 2].Text == "Handle" ||
                         workSheet.Cells[1, 3].Text == "Vendor" || workSheet.Cells[1, 4].Text == "Types" ||
                         workSheet.Cells[1, 5].Text == "Tags" || workSheet.Cells[1, 6].Text == "Description" ||
                         workSheet.Cells[1, 7].Text == "Status" || workSheet.Cells[1, 8].Text == "Category" ||
                         workSheet.Cells[1, 9].Text == "UnitPrice" || workSheet.Cells[1, 10].Text == "Stock" ||
                         workSheet.Cells[1, 11].Text == "SKU" || workSheet.Cells[1, 12].Text == "Weight" ||
                         workSheet.Cells[1, 13].Text == "Unit" || workSheet.Cells[1, 14].Text == "Code" ||
                         workSheet.Cells[1, 15].Text == "Policy" || workSheet.Cells[1, 16].Text == "VariantStatus" ||
                         workSheet.Cells[1, 17].Text == "VariantName" || workSheet.Cells[1, 18].Text == "VariantValue")
                            {
                                for (int row = start.Row + 1; row <= end.Row; row++)
                                {
                                    Product product = new Product();
                                    ProductVariant productVariant = new ProductVariant();
                                    Variant variant = new Variant();

                                    //ProductName
                                    try
                                    {
                                        product.ProductName = workSheet.Cells[row, 1].Text;
                                        _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.ProductName
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.ProductName
                                                + " caused and error." + "<br />";
                                        }
                                    }

                                    //Handle
                                    try
                                    {
                                        product.Handle = workSheet.Cells[row, 2].Text;
                                        _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (DbUpdateException dex)
                                    {
                                        errorCount++;
                                        if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                                        {
                                            feedBack += "Error: Record " + product.Handle +
                                                " was rejected as a duplicate." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Handle +
                                                " caused an error." + "<br />";
                                        }
                                        _context.Remove(product);
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.Handle
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Handle
                                                + " caused and error." + "<br />";
                                        }
                                    }

                                    //Vendor
                                    try
                                    {
                                        product.Vendor.Name = workSheet.Cells[row, 3].Text;
                                        _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (DbUpdateException dex)
                                    {
                                        errorCount++;
                                        if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                                        {
                                            feedBack += "Error: Record " + product.Vendor +
                                                " was rejected as a duplicate." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Vendor +
                                                " caused an error." + "<br />";
                                        }
                                        _context.Remove(product);
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.Vendor
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Vendor
                                                + " caused and error." + "<br />";
                                        }
                                    }

                                    //Type
                                    try
                                    {
                                        product.Type = workSheet.Cells[row, 4].Text;
                                        _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.Type
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Type
                                                + " caused and error." + "<br />";
                                        }
                                    }

                                    //Tag
                                    try
                                    {
                                        product.Tag = workSheet.Cells[row, 5].Text;
                                        _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.Tag
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Tag
                                                + " caused and error." + "<br />";
                                        }
                                    }

                                    //Description
                                    try
                                    {
                                        product.Handle = workSheet.Cells[row, 6].Text;
                                        _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.Description
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Description
                                                + " caused and error." + "<br />";
                                        }
                                    }

                                    //IsActive
                                    try
                                    {
                                        string cellText = workSheet.Cells[row, 7].Text.Trim().ToLower();
                                        product.IsActive = (cellText.Equals(true) || cellText.Equals(false)); _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.IsActive
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.IsActive
                                                + " caused and error." + "<br />";
                                        }
                                    }
                                    //Category
                                    try
                                    {
                                        product.Type = workSheet.Cells[row, 8].Text;
                                        _context.Products.Add(product);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + product.Category
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + product.Category
                                                + " caused and error." + "<br />";
                                        }
                                    }

                                    //UnitPrice
                                    try
                                    {
                                        productVariant.UnitPrice = Convert.ToDecimal(workSheet.Cells[row, 9].Value);
                                        _context.ProductVariants.Add(productVariant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.UnitPrice
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.UnitPrice
                                                + " caused an error." + "<br />";
                                        }
                                    }

                                    //Stock
                                    try
                                    {
                                        productVariant.StockQuantity = Convert.ToInt32(workSheet.Cells[row, 10].Value);
                                        _context.ProductVariants.Add(productVariant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.StockQuantity
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.StockQuantity
                                                + " caused an error." + "<br />";
                                        }
                                    }
                                    //SKU
                                    try
                                    {
                                        productVariant.SKU_ExternalID = workSheet.Cells[row, 11].Text;
                                        _context.ProductVariants.Add(productVariant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.SKU_ExternalID
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.SKU_ExternalID
                                                + " caused an error." + "<br />";
                                        }
                                    }
                                    //Weight
                                    try
                                    {
                                        productVariant.Weight = Convert.ToDecimal(workSheet.Cells[row, 12].Value);
                                        _context.ProductVariants.Add(productVariant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.Weight
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.Weight
                                                + " caused an error." + "<br />";
                                        }
                                    }
                                    //Unit
                                    try
                                    {
                                        string cellText = workSheet.Cells[row, 13].Text.Trim().ToString();
                                        bool isValid =
                                        cellText.Equals(nameof(ImperialUnits.lb), StringComparison.OrdinalIgnoreCase) ||
                                        cellText.Equals(nameof(ImperialUnits.oz), StringComparison.OrdinalIgnoreCase) ||
                                        cellText.Equals(nameof(ImperialUnits.floz), StringComparison.OrdinalIgnoreCase);
                                        _context.ProductVariants.Add(productVariant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.Unit
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.Unit
                                                + " caused an error." + "<br />";
                                        }
                                    }
                                    //Barcode
                                    try
                                    {
                                        productVariant.Barcode = workSheet.Cells[row, 14].Text;
                                        _context.ProductVariants.Add(productVariant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.Barcode
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.Barcode
                                                + " caused an error." + "<br />";
                                        }
                                    }

                                    //Policy
                                    try
                                    {
                                        string cellText = workSheet.Cells[row, 15].Text.Trim();
                                        bool isValid =
                                            cellText.Equals(nameof(InventoryPolicy.Deny), StringComparison.OrdinalIgnoreCase) ||
                                            cellText.Equals(nameof(InventoryPolicy.Continue), StringComparison.OrdinalIgnoreCase); 
                                        _context.ProductVariants.Add(productVariant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.InventoryPolicy
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.InventoryPolicy
                                                + " caused an error." + "<br />";
                                        }
                                    }
                                    //Status
                                    try
                                    {
                                        string cellText = workSheet.Cells[row, 16].Text.Trim().ToLower();
                                        productVariant.Status = (cellText.Equals(true) || cellText.Equals(false));
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + productVariant.Status
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + productVariant.Status
                                                + " caused an error." + "<br />";
                                        }
                                    }
                                    //VariantName
                                    try
                                    {
                                        variant.Name = workSheet.Cells[row, 17].Text;
                                        _context.Variants.Add(variant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + variant.Name
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + variant.Name
                                                + " caused an error." + "<br />";
                                        }
                                    }

                                    //VariantValuw
                                    try
                                    {
                                        variant.Value = workSheet.Cells[row, 18].Text;
                                        _context.Variants.Add(variant);
                                        _context.SaveChanges();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (ex.GetBaseException().Message.Contains("correct format"))
                                        {
                                            feedBack += "Error: Record " + variant.Value
                                                + " was rejected becuase it was not in the correct format." + "<br />";
                                        }
                                        else
                                        {
                                            feedBack += "Error: Record " + variant.Value
                                                + " caused an error." + "<br />";
                                        }
                                    }
                                }
                            }

                            else
                            {
                                feedBack += "Finished Importing " + (successCount + errorCount).ToString() +
                                    " Records with " + successCount.ToString() + " inserted and " +
                                    errorCount.ToString() + " rejected";

                                feedBack = "Error: You may have selected the wrong file to upload.<br /> " +
                                    "Remember, you must have the heading 'Type' in the " +
                                    "eighteenth cell of the first row.";
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        feedBack = "Error: That file is not an csv spreadsheet.";
                    }
                }
                else
                {
                    feedBack = "Error:  file appears to be empty";
                }
            }
            else
            {
                feedBack = "Error: No file uploaded";
            }

            TempData["Feedback"] = feedBack + "<br />";

            return RedirectToAction(nameof(Index));
        }

    }
}
