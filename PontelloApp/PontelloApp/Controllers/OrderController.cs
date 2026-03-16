using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using PontelloApp.Utilities;
using QuestPDF.Fluent;

namespace PontelloApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly PontelloAppContext _context;

        public OrderController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: /Order
        public async Task<IActionResult> Index(string? SearchString, int? OrderStatusID, OrderStatus? Status, DateTime? FromDate, DateTime? ToDate, int? page, int? pageSizeID, string? actionButton)
        {
            int dealerId = 1; // TODO: replace with current dealer/user

            ViewData["Filtering"] = "btn-outline-secondary";
            int numberFilters = 0;

            ViewData["OrderStatusID"] = OrderStatusSelectList(Status);

            var orders = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Shipping)
                .Where(o => o.DealerId == dealerId)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking();

            if (!String.IsNullOrEmpty(SearchString))
            {
                orders = orders.Where(o => o.PONumber.ToUpper().Contains(SearchString.ToUpper()));

                numberFilters++;
            }

            if (FromDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt >= FromDate);
                numberFilters++;
            }

            if (ToDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt <= ToDate);
                numberFilters++;
            }

            if (Status.HasValue)
            {
                orders = orders.Where(o => o.Status == Status.Value);
            }

            if (numberFilters != 0)
            {
                ViewData["numberFilters"] = "(" + numberFilters.ToString() + ")";
                ViewData["ShowFilter"] = "show";
            }

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Order");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            int totalOrders = orders.Count();
            ViewData["TotalOrders"] = totalOrders;

            var pagedData = await PaginatedList<Order>.CreateAsync(orders, page ?? 1, pageSize);

            return View(await orders.ToListAsync());


        }

        // GET: Admin management view for orders
        public async Task<IActionResult> Admin()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Shipping)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var pendingSchedules = await _context.RecurringOrders
                .Include(r => r.OriginalOrder)
                .Where(r => r.IsActive && r.NextRun > DateTime.Now)
                .ToListAsync();

            ViewBag.PendingSchedules = pendingSchedules;

            return View(orders);
        }

        // GET: /Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /Order/Review/5
        public async Task<IActionResult> Review(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(i => i.Options)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        private async Task<Order?> GetOrder(int id)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Options)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IActionResult> Progress(int id)
        {
            var order = await GetOrder(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Approved(int id)
        {
            var order = await GetOrder(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Rejected(int id)
        {
            var order = await GetOrder(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Shipped(int id)
        {
            var order = await GetOrder(id);
            if (order == null) return NotFound();
            return View(order);
        }
        public async Task<IActionResult> Recurring(int id)
        {
            var order = await GetOrder(id);
            if (order == null) return NotFound();
            return View(order);
        }
        public IActionResult ExportOrderPO(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return NotFound();

            var items = order.Items.Select(i => new
            {
                Product = i.Product.ProductName,
                Quantity = i.Quantity,
                Price = i.UnitPrice,
                Total = i.Quantity * i.UnitPrice
            }).ToList();

            decimal subtotal = items.Sum(i => i.Total);
            decimal tax = order.TaxAmount;
            decimal shippingCost = order.Shipping?.ShippingCost ?? 0m;
            decimal grandTotal = order.TotalAmount; // order.TotalAmount should include shipping if saved

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // HEADER
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Pontello").FontSize(20).Bold();
                            col.Item().Text("Purchase Order").FontSize(14);
                        });

                        row.ConstantItem(200).AlignRight().Column(col =>
                        {
                            col.Item().Text($"PO #: {order.PONumber}").Bold();
                            col.Item().Text($"Date: {order.CreatedAt:yyyy-MM-dd}");
                        });
                    });

                    // CONTENT
                    page.Content().PaddingVertical(15).Column(col =>
                    {

                        // SHIPPING INFO
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Ship To").Bold();
                                c.Item().Text(order.Shipping?.FullName ?? "");
                                c.Item().Text(order.Shipping?.FullAddress ?? "N/A");
                                c.Item().Text(order.Shipping?.Email ?? "");
                                c.Item().Text(order.Shipping?.Phone ?? "");

                                if (!string.IsNullOrWhiteSpace(order.Shipping?.BinOrEin))
                                    c.Item().Text($"BIN: {order.Shipping.BinOrEin}");

                                if (!string.IsNullOrWhiteSpace(order.Shipping?.TrackingNumber))
                                    c.Item().Text($"Tracking #: {order.Shipping.TrackingNumber}");
                            });
                        });

                        col.Item().PaddingVertical(10);

                        // TABLE
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(5);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Product").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Qty").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Unit Price").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Total").Bold();
                            });

                            foreach (var i in items)
                            {
                                table.Cell().Padding(5).Text(i.Product);
                                table.Cell().Padding(5).Text(i.Quantity.ToString());
                                table.Cell().Padding(5).Text("$" + i.Price.ToString("0.00"));
                                table.Cell().Padding(5).Text("$" + i.Total.ToString("0.00"));
                            }
                        });

                        col.Item().PaddingTop(15);

                        // TOTALS
                        col.Item().AlignRight().Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Subtotal:");
                                r.ConstantItem(100).AlignRight().Text("$" + subtotal.ToString("0.00"));
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Tax:");
                                r.ConstantItem(100).AlignRight().Text("$" + tax.ToString("0.00"));
                            });

                            if (shippingCost > 0)
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().AlignRight().Text("Shipping:");
                                    r.ConstantItem(100).AlignRight().Text("$" + shippingCost.ToString("0.00"));
                                });
                            }

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Total:").Bold();
                                r.ConstantItem(100).AlignRight().Text("$" + grandTotal.ToString("0.00")).Bold();
                            });
                        });
                    });

                    // FOOTER
                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(10)
                        .FontColor("#777777");
                });

            }).GeneratePdf();

            return File(pdf, "application/pdf", $"Purchase Order .pdf");
        }

        private SelectList OrderStatusSelectList(OrderStatus? selectedStatus)
        {
            var statusList = Enum.GetValues(typeof(OrderStatus))
                                 .Cast<OrderStatus>()
                                 .Select(s => new
                                 {
                                     Value = s,
                                     Text = s.ToString()
                                 });

            return new SelectList(statusList, "Value", "Text", selectedStatus);
        }

        public async Task<IActionResult> Decision(int id, string status)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == id &&
                    (o.Status == OrderStatus.Submitted || o.Status == OrderStatus.Approved));

            if (order == null || order.Items == null || !order.Items.Any())
                return RedirectToAction("Action", "Order");

            // generate PO, keep status as Draft until shipping is provided
            order.PONumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}";

            order.CreatedAt = DateTime.Now;


            if (status == "Approved")
            {
                order.Status = OrderStatus.Approved;
            }

            // persist the created order (with its items and shipping placeholder)
            await _context.SaveChangesAsync();

            return RedirectToAction("Admin", "Order");
        }

        [HttpPost]
        public async Task<IActionResult> ShipOrder(int id, string TrackingNumber, decimal ShippingCost)
        {
            var order = await _context.Orders
                .Include(o => o.Shipping)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.Shipping == null)
                order.Shipping = new Shipping();

            order.Shipping.TrackingNumber = TrackingNumber;
            order.Shipping.ShippingCost = ShippingCost;
            order.TotalAmount += ShippingCost; // Add shipping cost to total

            order.Status = OrderStatus.Shipped;

            // Recalculate totals including shipping
            var subtotal = order.Items?.Sum(i => i.TotalPrice) ?? 0m;

            // Keep existing tax amount (tax already calculated when shipping was first saved). If you need to recalc,
            // you can adopt the same BIN/EIN logic used elsewhere. Here we preserve order.TaxAmount.
            var tax = order.TaxAmount;

            order.TotalAmount = Math.Round(subtotal + tax + (order.Shipping?.ShippingCost ?? 0m), 2);

            await _context.SaveChangesAsync();

            return RedirectToAction("Admin");
        }


        [HttpPost]
        public async Task<IActionResult> RejectOrder(int id, string reason)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            order.Status = OrderStatus.Rejected;
            order.RejectReason = reason;

            await _context.SaveChangesAsync();

            return RedirectToAction("Admin");
        }
    }
}






