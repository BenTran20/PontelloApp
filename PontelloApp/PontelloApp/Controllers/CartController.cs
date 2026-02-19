using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using PontelloApp.Ultilities;

namespace PontelloApp.Controllers
{
    public class CartController : Controller
    {
        private readonly PontelloAppContext _context;

        public CartController(PontelloAppContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Cart()
        {
            int dealerId = 1;
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                .ThenInclude(i => i.Options)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.DealerId == dealerId && o.Status == OrderStatus.Draft);

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCart(int id, int quantity)
        {
            var item = await _context.OrderItems
                .Include(i => i.Order)
                .ThenInclude(o => o.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (quantity <= 0)
            {
                _context.OrderItems.Remove(item);
            }

            if (item != null)
            {
                item.Quantity = quantity;

                var order = item.Order;

                order.TotalAmount = order.Items.Sum(x => x.TotalPrice);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Cart");
        }


        [HttpPost]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            var cart = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(o => o.Status == OrderStatus.Draft /* && o.DealerId == currentDealerId */);

            if (cart == null)
            {
                TempData["ErrorMessage"] = "Cart not found.";
                return RedirectToAction("Cart");
            }

            var item = cart.Items.FirstOrDefault(x => x.Id == itemId);
            if (item != null)
            {
                cart.Items.Remove(item);

                cart.TotalAmount = cart.Items.Sum(x => x.TotalPrice);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Item removed from cart.";
            }
            else
            {
                TempData["ErrorMessage"] = "Item not found.";
            }

            return RedirectToAction("Cart");
        }

        // POST: create/submit the order only, then redirect to Shipping controller to collect shipping info
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id)
        {
            var cart = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == OrderStatus.Draft);

            if (cart == null || cart.Items == null || !cart.Items.Any())
                return RedirectToAction("Cart");

            cart.PONumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}";
            cart.Status = OrderStatus.Submitted;
            cart.CreatedAt = DateTime.Now;

            cart.TaxAmount = Math.Round(cart.Items.Sum(i => i.TotalPrice) * 0.13m, 2);
            cart.TotalAmount = cart.Items.Sum(i => i.TotalPrice) + cart.TaxAmount;

            await _context.SaveChangesAsync();

            // show success message at the top of the shipping form
            TempData["SuccessMessage"] = "Order created successfully.";

            return RedirectToAction("Create", "Shipping", new { orderId = cart.Id });
        }

    }
}
