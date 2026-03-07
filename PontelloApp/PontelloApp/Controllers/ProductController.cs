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
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Text;

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
                IsActive = true,
                IsUnlisted = false
            };

            PopulateDropDownLists();
            return View(product);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,Handle,VendorID,Type,Tag,Description,IsActive,IsUnlisted,CategoryID")] Product product)
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
                p => p.ProductName, p => p.Description, p => p.IsActive, p => p.IsUnlisted, p => p.CategoryID,
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
            var productVariants = _context.ProductVariants
                .Include(pv => pv.Product)
                    .ThenInclude(p => p.Vendor)
                .Include(pv => pv.Product)
                    .ThenInclude(p => p.Category)
                .Include(pv => pv.Options)
                .AsNoTracking()
                .OrderBy(pv => pv.Product.ProductName)
                .ToList();

            if (!productVariants.Any())
                return NotFound("No data.");

            var sb = new StringBuilder();

            // CSV Header
            sb.AppendLine("Product,Handle,Vendor,Types,Tags,Description,Status,Unlisted,Category,UnitPrice,CostPrice,ComparePrice,Stock,SKU,Weight,Unit,Code,Policy,VariantStatus,VariantName1,VariantValue1,VariantName2,VariantValue2,VariantName3,VariantValue3");

            foreach (var pv in productVariants)
            {
                var options = pv.Options.Take(3).ToList();

                string variantName1 = options.Count > 0 ? options[0].Name ?? "" : "";
                string variantValue1 = options.Count > 0 ? options[0].Value ?? "" : "";

                string variantName2 = options.Count > 1 ? options[1].Name ?? "" : "";
                string variantValue2 = options.Count > 1 ? options[1].Value ?? "" : "";

                string variantName3 = options.Count > 2 ? options[2].Name ?? "" : "";
                string variantValue3 = options.Count > 2 ? options[2].Value ?? "" : "";

                sb.AppendLine(string.Join(",", new[]
                {
            CsvEscape(pv.Product.ProductName),
            CsvEscape(pv.Product.Handle),
            CsvEscape(pv.Product.Vendor?.Name ?? ""),
            CsvEscape(pv.Product.Type),
            CsvEscape(pv.Product.Tag),
            CsvEscape(pv.Product.Description),
            CsvEscape(pv.Product.IsActive.ToString()),
            CsvEscape(pv.Product.IsUnlisted.ToString()),
            CsvEscape(pv.Product.Category?.FullCategory ?? ""),
            CsvEscape(pv.UnitPrice.ToString()),
            CsvEscape(pv.CostPrice?.ToString() ?? ""),
            CsvEscape(pv.CompareAtPrice?.ToString() ?? ""),
            CsvEscape(pv.StockQuantity.ToString()),
            CsvEscape(pv.SKU_ExternalID ?? ""),
            CsvEscape(pv.Weight?.ToString() ?? ""),
            CsvEscape(pv.Unit.ToString()),
            CsvEscape(pv.Barcode ?? ""),
            CsvEscape(pv.InventoryPolicy?.ToString() ?? ""),
            CsvEscape(pv.Status.ToString()),
            CsvEscape(variantName1),
            CsvEscape(variantValue1),
            CsvEscape(variantName2),
            CsvEscape(variantValue2),
            CsvEscape(variantName3),
            CsvEscape(variantValue3)
        }));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "PontelloProducts.csv");
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
        public async Task<IActionResult> InsertFromCsv(IFormFile csvFile)
        {
            string feedback = string.Empty;

            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["Feedback"] = "Error: No file uploaded.<br/>";
                return RedirectToAction(nameof(Create));
            }

            if (!csvFile.FileName.EndsWith(".csv"))
            {
                TempData["Feedback"] = "Error: The file must be a CSV.<br/>";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var workSheet = package.Workbook.Worksheets.Add("TempSheet");

                    using (var reader = new StreamReader(csvFile.OpenReadStream()))
                    {
                        string csvText = await reader.ReadToEndAsync();
                        var format = new ExcelTextFormat
                        {
                            Delimiter = ',',
                            TextQualifier = '"'
                        };
                        workSheet.Cells.LoadFromText(csvText, format);
                    }

                    var start = workSheet.Dimension.Start;
                    var end = workSheet.Dimension.End;

                    int successCount = 0;
                    int errorCount = 0;

                    // Optional: Validate headers first
                    var headers = new[]
                    {
                        "Product","Handle","Vendor","Types","Tags","Description",
                        "Status","Unlisted","Category","UnitPrice","CostPrice","ComparePrice",
                        "Stock","SKU","Weight","Unit","Barcode","Policy","VariantStatus",
                        "VariantName1","VariantValue1","VariantName2","VariantValue2",
                        "VariantName3","VariantValue3"
                    };

                    bool headersValid = true;
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        if (!string.Equals(workSheet.Cells[1, col].Text.Trim(), headers[col - 1], StringComparison.OrdinalIgnoreCase))
                        {
                            headersValid = false;
                            break;
                        }
                    }

                    if (!headersValid)
                    {
                        TempData["Feedback"] = "Error: CSV headers incorrect.<br/>";
                        return RedirectToAction(nameof(Create));
                    }

                    // Process rows
                    for (int row = start.Row + 1; row < end.Row; row++)
                    {
                        List<string> rowErrors = new List<string>();

                        // Read all cell values
                        string productName = workSheet.Cells[row, 1].Text.Trim();
                        string handle = workSheet.Cells[row, 2].Text.Trim();
                        string vendorName = workSheet.Cells[row, 3].Text.Trim();
                        string type = workSheet.Cells[row, 4].Text.Trim();
                        string tag = workSheet.Cells[row, 5].Text.Trim();
                        string description = workSheet.Cells[row, 6].Text.Trim();
                        string statusText = workSheet.Cells[row, 7].Text.Trim();
                        string unlistedText = workSheet.Cells[row, 8].Text.Trim();
                        string categoryName = workSheet.Cells[row, 9].Text.Trim();
                        string priceText = workSheet.Cells[row, 10].Text.Trim();
                        string costPriceText = workSheet.Cells[row, 11].Text.Trim();
                        string comparePriceText = workSheet.Cells[row, 12].Text.Trim();
                        string stockText = workSheet.Cells[row, 13].Text.Trim();
                        string sku = workSheet.Cells[row, 14].Text.Trim();
                        string weightText = workSheet.Cells[row, 15].Text.Trim();
                        string unitText = workSheet.Cells[row, 16].Text.Trim();
                        string barcode = workSheet.Cells[row, 17].Text.Trim();
                        string policy = workSheet.Cells[row, 18].Text.Trim();
                        string status = workSheet.Cells[row, 19].Text.Trim();
                        string variantName1 = workSheet.Cells[row, 20].Text.Trim();
                        string variantValue1 = workSheet.Cells[row, 21].Text.Trim();
                        string variantName2 = workSheet.Cells[row, 22].Text.Trim();
                        string variantValue2 = workSheet.Cells[row, 23].Text.Trim();
                        string variantName3 = workSheet.Cells[row, 24].Text.Trim();
                        string variantValue3 = workSheet.Cells[row, 25].Text.Trim();

                        // Parse numbers
                        decimal.TryParse(priceText, out decimal price);
                        decimal.TryParse(costPriceText, out decimal costPrice);
                        decimal.TryParse(comparePriceText, out decimal comparePrice);
                        decimal.TryParse(weightText, out decimal weight);
                        int.TryParse(stockText, out int stock);

                        // Parse unit
                        ImperialUnits unit = unitText.ToLower() switch
                        {
                            "oz" => ImperialUnits.oz,
                            "floz" or "fl oz" => ImperialUnits.floz,
                            "lb" or "lbs" => ImperialUnits.lb,
                            _ => ImperialUnits.oz
                        };
                        if (!Enum.IsDefined(typeof(ImperialUnits), unit))
                            rowErrors.Add($"Invalid Unit '{unitText}'");

                        // Validate required fields
                        if (string.IsNullOrEmpty(productName)) rowErrors.Add("ProductName missing");
                        if (string.IsNullOrEmpty(handle)) rowErrors.Add("Handle missing");
                        if (!decimal.TryParse(priceText, out _)) rowErrors.Add("UnitPrice invalid");
                        if (!int.TryParse(stockText, out _)) rowErrors.Add("Stock invalid");

                        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Name.ToLower() == vendorName.ToLower());
                        if (vendor == null) rowErrors.Add($"Vendor '{vendorName}' not found");

                        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                        if (category == null) rowErrors.Add($"Category '{categoryName}' not found");

                        bool isActive = statusText.ToLower() == "true" || statusText.ToLower() == "active";
                        bool isUnlisted = unlistedText.ToLower() == "true";

                        if (rowErrors.Any())
                        {
                            errorCount++;
                            feedback += $"Row {row}: {string.Join(", ", rowErrors)} <br/>";
                            continue;
                        }

                        try
                        {
                            // Insert product
                            Product product = await _context.Products.FirstOrDefaultAsync(p => p.Handle == handle)
                                              ?? new Product
                                              {
                                                  ProductName = productName,
                                                  Handle = handle,
                                                  VendorID = vendor.VendorID,
                                                  Type = type,
                                                  Tag = tag,
                                                  Description = description,
                                                  CategoryID = category.ID,
                                                  IsActive = isActive,
                                                  IsUnlisted = isUnlisted
                                              };

                            if (product.ID == 0) _context.Products.Add(product);
                            await _context.SaveChangesAsync();

                            // Insert variant
                            ProductVariant variant = new ProductVariant
                            {
                                ProductId = product.ID,
                                UnitPrice = price,
                                CostPrice = costPrice,
                                CompareAtPrice = comparePrice,
                                StockQuantity = stock,
                                SKU_ExternalID = sku,
                                Weight = weight,
                                Barcode = barcode,
                                InventoryPolicy = policy.ToLower() == "deny" ? InventoryPolicy.Deny : InventoryPolicy.Continue,
                                Unit = unit,
                                Status = status.ToLower() == "true" || status.ToLower() == "active"
                            };

                            _context.ProductVariants.Add(variant);
                            await _context.SaveChangesAsync();

                            // Insert variant options
                            var options = new List<Variant>();
                            if (!string.IsNullOrEmpty(variantName1)) options.Add(new Variant { ProductVariantId = variant.Id, Name = variantName1, Value = variantValue1 });
                            if (!string.IsNullOrEmpty(variantName2)) options.Add(new Variant { ProductVariantId = variant.Id, Name = variantName2, Value = variantValue2 });
                            if (!string.IsNullOrEmpty(variantName3)) options.Add(new Variant { ProductVariantId = variant.Id, Name = variantName3, Value = variantValue3 });

                            if (options.Any())
                            {
                                _context.Variants.AddRange(options);
                                await _context.SaveChangesAsync();
                            }

                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            feedback += $"Row {row}: Error - {ex.Message}<br/>";
                        }
                    }

                    feedback = $"Finished importing {successCount + errorCount} records: {successCount} inserted, {errorCount} rejected.<br/>{feedback}";
                }
            }
            catch (Exception ex)
            {
                feedback = $"Error reading CSV file: {ex.Message}<br/>";
            }

            TempData["Feedback"] = feedback;
            return RedirectToAction(nameof(Create));
        }
    }
}
