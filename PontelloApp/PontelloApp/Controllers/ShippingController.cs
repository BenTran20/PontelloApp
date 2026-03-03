using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PontelloApp.Controllers
{
    public class ShippingController : Controller
    {
        private readonly PontelloAppContext _context;

        public ShippingController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: Shipping/Create?orderId=123
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Submitted);

            if (order == null)
                return RedirectToAction("Cart", "Cart");

            return View(order);
        }

        // POST: Shipping/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderId, string address, string phone, string email, string? binOrEin)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Submitted);

            if (order == null)
                return RedirectToAction("Cart", "Cart");

            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Please fill all shipping fields.");
                return View(order);
            }

            if (order.Shipping == null)
            {
                order.Shipping = new Shipping
                {
                    Address = address,
                    Phone = phone,
                    Email = email,
                    BinOrEin = string.IsNullOrWhiteSpace(binOrEin) ? null : binOrEin
                };
            }
            else
            {
                order.Shipping.Address = address;
                order.Shipping.Phone = phone;
                order.Shipping.Email = email;
                order.Shipping.BinOrEin = string.IsNullOrWhiteSpace(binOrEin) ? null : binOrEin;
            }

            // Recalculate tax and total on the order immediately so the saved order reflects exemption
            var subtotal = order.Items?.Sum(i => i.TotalPrice) ?? 0m;
            if (!string.IsNullOrWhiteSpace(order.Shipping?.BinOrEin))
            {
                order.TaxAmount = 0m;
            }
            else
            {
                order.TaxAmount = Math.Round(subtotal * 0.13m, 2);
            }

            order.TotalAmount = Math.Round(subtotal + order.TaxAmount, 2);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Shipping info saved successfully.";

            return RedirectToAction("Details", "Order", new { id = order.Id });
        }
    }
}
