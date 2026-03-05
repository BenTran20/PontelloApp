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

        // Ensure a Shipping placeholder exists for the order (prevents null ref and gives a place to fill BIN/EIN)
        private void EnsureShippingPlaceholder(Order order)
        {
            if (order == null) return;
            if (order.Shipping != null) return;

            var shipping = new Shipping
            {
                Address = string.Empty,
                Phone = string.Empty,
                Email = string.Empty,
                OrderId = order.Id
            };

            // Track the new shipping and attach to the order
            _context.Shippings.Add(shipping);
            order.Shipping = shipping;
        }

        // POST: create the order (keeps it in Draft) then redirect to Shipping controller to collect shipping info
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

            // generate PO, keep status as Draft until shipping is provided
            cart.PONumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}";

            cart.Status = OrderStatus.Progress;
            cart.CreatedAt = DateTime.Now;

            // create a shipping placeholder so Shipping view/controller always has an object to update (including BIN/EIN)
            if (cart.Shipping == null)
            {
                EnsureShippingPlaceholder(cart);
            }

            // calculate current tax/total for informational purposes (will be recalculated when shipping saved)
            cart.TaxAmount = Math.Round(cart.Items.Sum(i => i.TotalPrice) * 0.13m, 2);
            cart.TotalAmount = cart.Items.Sum(i => i.TotalPrice) + cart.TaxAmount;

            // persist the created order (with its items and shipping placeholder)
            await _context.SaveChangesAsync();

            // show success message at the top of the shipping form
            TempData["SuccessMessage"] = "Order created. Please provide shipping to complete submission.";

            return RedirectToAction("Create", "Shipping", new { orderId = cart.Id });
        }

    }
}
