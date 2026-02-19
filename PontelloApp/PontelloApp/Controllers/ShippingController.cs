using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

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
 public async Task<IActionResult> Create(int orderId, string address, string phone, string email)
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
 Email = email
 };
 }
 else
 {
 order.Shipping.Address = address;
 order.Shipping.Phone = phone;
 order.Shipping.Email = email;
 }

 await _context.SaveChangesAsync();

 TempData["SuccessMessage"] = "Shipping info saved successfully.";

 return RedirectToAction("Details", "Order", new { id = order.Id });
 }
 }
}
