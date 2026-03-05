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
            int dealerId =1; // TODO: replace with current dealer/user

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

    }
}
