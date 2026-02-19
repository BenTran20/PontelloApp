using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;

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
        public async Task<IActionResult> Index()
        {
            int dealerId =1; // TODO: replace with current dealer/user

            var orders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Shipping)
                .Where(o => o.DealerId == dealerId && o.Status != Models.OrderStatus.Draft)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

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

    }
}
