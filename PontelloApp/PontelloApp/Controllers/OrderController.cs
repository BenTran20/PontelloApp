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

        // GET: /Order/Confirm/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

    }
}
