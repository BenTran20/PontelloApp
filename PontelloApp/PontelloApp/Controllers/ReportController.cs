using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using QuestPDF.Fluent;

namespace PontelloApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly PontelloAppContext _context;

        public ReportController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: /Report
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Report/SalesReport
        public async Task<IActionResult> SalesReport(DateTime? fromDate, DateTime? toDate, OrderStatus? status)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Shipping)
                .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Progress)
                .AsQueryable();

            var validRevenueStatuses = new[] {
                OrderStatus.Submitted,
                OrderStatus.Approved,
                OrderStatus.Shipped
            };

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["Status"] = status;
            ViewData["TotalOrders"] = orders.Count;
            ViewData["TotalRevenue"] = orders
                    .Where(o => validRevenueStatuses.Contains(o.Status))
                    .Sum(o => o.TotalAmount);
            ViewData["StatusList"] = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().ToList();

            return View(orders);
        }

        // GET: /Report/ExportSalesCsv
        public async Task<IActionResult> ExportSalesCsv(DateTime? fromDate, DateTime? toDate, OrderStatus? status)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Shipping)
                .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Progress)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));
            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("PO Number,Date,Status,Customer,Total Amount,Tax,Items");

            foreach (var o in orders)
            {
                var items = string.Join("; ", o.Items?.Select(i => $"{i.Product?.ProductName} x{i.Quantity}") ?? []);
                csv.AppendLine($"{o.PONumber},{o.CreatedAt:yyyy-MM-dd},{o.Status},{o.Shipping?.FullName ?? "N/A"},${o.TotalAmount:0.00},${o.TaxAmount:0.00},\"{items}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"SalesReport_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: /Report/ExportSalesPdf
        public async Task<IActionResult> ExportSalesPdf(DateTime? fromDate, DateTime? toDate, OrderStatus? status)
        {
            var query = _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Shipping)
            .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Progress)
            .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));
            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            decimal totalRevenue = orders
                .Where(o => o.Status == OrderStatus.Shipped)
                .Sum(o => o.TotalAmount);

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Pontello — Sales Report").FontSize(20).Bold();
                        col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(10).FontColor("#777777");
                        if (fromDate.HasValue || toDate.HasValue)
                            col.Item().Text($"Period: {fromDate?.ToString("yyyy-MM-dd") ?? "All"} → {toDate?.ToString("yyyy-MM-dd") ?? "All"}").FontSize(10);
                        if (status.HasValue)
                            col.Item().Text($"Status: {status}").FontSize(10);
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        // Summary
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#EEEEEE").Padding(10).Column(c =>
                            {
                                c.Item().Text("Total Orders").FontSize(10).FontColor("#777");
                                c.Item().Text(orders.Count.ToString()).FontSize(18).Bold();
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Border(1).BorderColor("#EEEEEE").Padding(10).Column(c =>
                            {
                                c.Item().Text("Total Revenue").FontSize(10).FontColor("#777");
                                c.Item().Text($"${totalRevenue:0.00}").FontSize(18).Bold();
                            });
                        });

                        col.Item().PaddingVertical(10);

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#F3F4F6").Padding(6).Text("PO Number").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Date").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Customer").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Status").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Total").Bold();
                            });

                            foreach (var o in orders)
                            {
                                table.Cell().Padding(5).Text(o.PONumber);
                                table.Cell().Padding(5).Text(o.CreatedAt.ToString("yyyy-MM-dd"));
                                table.Cell().Padding(5).Text(o.Shipping?.FullName ?? "N/A");
                                table.Cell().Padding(5).Text(o.Status.ToString());
                                table.Cell().Padding(5).Text("$" + o.TotalAmount.ToString("0.00"));
                            }
                        });

                        col.Item().PaddingTop(10).AlignRight()
                            .Text($"Total Revenue: ${totalRevenue:0.00}").Bold().FontSize(12);
                    });

                    page.Footer().AlignCenter()
                        .Text($"Pontello Sales Report — {DateTime.Now:yyyy-MM-dd}")
                        .FontSize(9).FontColor("#999999");
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"SalesReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> PrintOrder(string poNumber)
        {
            if (string.IsNullOrWhiteSpace(poNumber))
                return RedirectToAction("Index");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PONumber == poNumber);

            if (order == null)
            {
                TempData["Error"] = $"Order '{poNumber}' not found.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("ExportOrderPO", "Order", new { id = order.Id });
        }
    }
}