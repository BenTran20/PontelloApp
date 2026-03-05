using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using PontelloApp.Utilities;

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
                .Where(o => o.DealerId == dealerId && o.Status != Models.OrderStatus.Draft)
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

            return View(pagedData);


            return View(orders);
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id)
        {
            int dealerId = 1;
            var source = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.DealerId == dealerId);

            if (source == null)
            {
                TempData["Error"] = "The Order is unavailable";
                return RedirectToAction(nameof(Index));
            }


            if (!(source.Status == OrderStatus.Approved || source.Status == OrderStatus.Shipped))
            {
                TempData["Error"] = $"Cannot reorder from a {source.Status} order.";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var draft = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.DealerId == dealerId && o.Status == OrderStatus.Draft);

                if (draft == null)
                {
                    draft = new Order
                    {
                        DealerId = dealerId,
                        Status = OrderStatus.Draft,
                        PONumber = string.Empty,
                        RevisionNumber = 0,
                        TaxAmount = 0,
                        TotalAmount = 0,
                        CreatedAt = DateTime.Now
                    };
                    _context.Orders.Add(draft);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (draft.Items != null && draft.Items.Any())
                    {
                        _context.OrderItems.RemoveRange(draft.Items);
                        draft.Items.Clear();
                    }
                    draft.TaxAmount = 0;
                    draft.TotalAmount = 0;
                }


                if (source.Items != null)
                {
                    foreach (var oi in source.Items)
                    {
                        var product = oi.Product;
                        if (product == null || (product is { IsActive: false }))
                        {
                            var name = product?.ProductName ?? $"Product #{oi.ProductId}";
                            TempData["ReorderWarnings"] = "Item \"{name}\" is no longer available and was skipped.";
                            continue;
                        }

                        var newItem = new OrderItem
                        {
                            OrderId = draft.Id,
                            ProductId = oi.ProductId,
                            ProductVariantId = oi.ProductVariantId,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice
                        };
                        _context.OrderItems.Add(newItem);
                    }
                }

                await _context.SaveChangesAsync();

                await RecalculateTotalsAsync(draft.Id);

                await tx.CommitAsync();

                TempData["SuccessMessage"] = "Reorder successful. You can edit your shopping cart before checkout.";
                return RedirectToAction("Cart", "Cart");
            }

            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Concurrency error. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }

            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Cannot Reorder: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

        }

        private async Task RecalculateTotalsAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstAsync(o => o.Id == orderId);

            var items = order.Items ?? new List<OrderItem>();
            var subtotal = items.Sum(i => i.TotalPrice);

            order.TaxAmount = Math.Round(subtotal * 0.13m, 2);
            order.TotalAmount = subtotal + order.TaxAmount;

            await _context.SaveChangesAsync();
        }

    }
}
