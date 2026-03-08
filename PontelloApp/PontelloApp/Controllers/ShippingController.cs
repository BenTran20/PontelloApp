using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using PontelloApp.Ultilities;
using QuestPDF.Fluent;
using System.Linq;
using System.Threading.Tasks;

namespace PontelloApp.Controllers
{
    public class ShippingController : Controller
    {
        private readonly PontelloAppContext _context;
        private readonly EmailSender _emailSender;

        public ShippingController(PontelloAppContext context, EmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Shipping/Create?orderId=123
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == orderId && (o.Status == OrderStatus.Draft
                || o.Status == OrderStatus.Progress || o.Status == OrderStatus.Submitted));

            if (order == null)
                return RedirectToAction("Cart", "Cart");

            return View(order);
        }

        // POST: Shipping/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderId, string fullName, string address, string phone, string email, string? binOrEin)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == orderId && (o.Status == OrderStatus.Draft ||
                   o.Status == OrderStatus.Progress || o.Status == OrderStatus.Submitted));

            if (order == null)
                return RedirectToAction("Cart", "Cart");

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Please fill all required shipping fields.");
                return View(order);
            }

            if (order.Shipping == null)
            {
                order.Shipping = new Shipping
                {
                    FullName = fullName,
                    Address = address,
                    Phone = phone,
                    Email = email,
                    BinOrEin = string.IsNullOrWhiteSpace(binOrEin) ? null : binOrEin
                };
            }
            else
            {
                order.Shipping.FullName = fullName;
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

            // We will mark submitted now that shipping is provided.
            // Before persisting, decrement stock for each variant in an atomic transaction.
            // Aggregate quantities by variant id to avoid double-check races within the order
            var variantQuantities = order.Items?
                .Where(i => i.ProductVariantId.HasValue)
                .GroupBy(i => i.ProductVariantId!.Value)
                .Select(g => new { VariantId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList() ?? new();

            // Start transaction so stock updates + order status change are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // For each variant referenced in the order, load tracked entity and decrement.
                // Important: Do NOT decrement stock for special-order variants (InventoryPolicy == Continue).
                foreach (var vq in variantQuantities)
                {
                    var variant = await _context.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == vq.VariantId);

                    if (variant == null)
                    {
                        ModelState.AddModelError(string.Empty, $"Product variant (ID {vq.VariantId}) not found. Please review your cart.");
                        await transaction.RollbackAsync();
                        return View(order);
                    }

                    // Treat null policy as Deny (conservative)
                    var policy = variant.InventoryPolicy ?? InventoryPolicy.Deny;

                    // If variant is special-order (Continue), skip local stock checks and decrement.
                    if (policy == InventoryPolicy.Continue)
                    {
                        // Special-order items are fulfilled externally; do not touch local stock.
                        continue;
                    }

                    // For Deny (normal inventory) enforce stock availability
                    if (variant.StockQuantity < vq.Quantity)
                    {
                        ModelState.AddModelError(string.Empty, $"Insufficient stock for variant '{variant.SKU_ExternalID ?? variant.Id.ToString()}'. Available: {variant.StockQuantity}, requested: {vq.Quantity}.");
                        await transaction.RollbackAsync();
                        return View(order);
                    }

                    variant.StockQuantity -= vq.Quantity;
                    _context.ProductVariants.Update(variant);
                }

                // mark submitted now shipping provided
                order.Status = OrderStatus.Submitted;

                await _context.SaveChangesAsync();

                // GENERATE PDF
                byte[] pdfBytes;
                {
                    var items = order.Items.Select(i => new
                    {
                        Product = i.Product.ProductName,
                        Quantity = i.Quantity,
                        Price = i.UnitPrice,
                        Total = i.Quantity * i.UnitPrice
                    }).ToList();

                    decimal subtotalCalc = items.Sum(i => i.Total);
                    decimal taxCalc = order.TaxAmount;
                    decimal grandTotal = order.TotalAmount;

                    pdfBytes = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Margin(30);

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

                            page.Content().PaddingVertical(15).Column(col =>
                            {
                                col.Item().Text("Ship To").Bold();
                                col.Item().Text(order.Shipping?.FullName ?? "");
                                col.Item().Text(order.Shipping?.Address ?? "N/A");
                                col.Item().Text(order.Shipping?.Email ?? "");
                                col.Item().Text(order.Shipping?.Phone ?? "");

                                col.Item().PaddingVertical(10);

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

                                col.Item().PaddingTop(15).AlignRight().Column(c =>
                                {
                                    c.Item().Row(r =>
                                    {
                                        r.RelativeItem().AlignRight().Text("Subtotal:");
                                        r.ConstantItem(100).AlignRight().Text("$" + subtotalCalc.ToString("0.00"));
                                    });
                                    c.Item().Row(r =>
                                    {
                                        r.RelativeItem().AlignRight().Text("Tax:");
                                        r.ConstantItem(100).AlignRight().Text("$" + taxCalc.ToString("0.00"));
                                    });
                                    c.Item().Row(r =>
                                    {
                                        r.RelativeItem().AlignRight().Text("Total:").Bold();
                                        r.ConstantItem(100).AlignRight().Text("$" + grandTotal.ToString("0.00")).Bold();
                                    });
                                });
                            });

                            page.Footer()
                                .AlignCenter()
                                .Text($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(10)
                                .FontColor("#777777");
                        });
                    }).GeneratePdf();

                    if (!string.IsNullOrWhiteSpace(order.Shipping?.Email))
                    {
                        string subject = $"Your Pontello Order {order.PONumber}";
                        string body = $@"
                            <div style=""font-family: Arial, sans-serif; font-size: 14px; color: #333; text-align: left;"">

                            <p>Hi <strong>{order.Shipping.FullName}</strong>,</p>

                            <p>Thank you for your order! We're excited to let you know that your purchase has been received and is being processed.</p>

                            <p>You can find your Purchase Order attached for your reference.</p>

                            <hr style=""border:none; border-top:1px solid #eee; margin:20px 0;"" />

                            <p style=""font-size:12px; color:#777;"">
                                Pontello Team<br/>
                                Questions? Reply to this email 
                            </p>
                        </div>";

                        // Save pdf temporarily
                        string tempPath = Path.Combine(Path.GetTempPath(), $"PO_{order.PONumber}.pdf");
                        await System.IO.File.WriteAllBytesAsync(tempPath, pdfBytes);

                        await _emailSender.SendEmailWithAttachmentAsync(order.Shipping.Email, subject, body, tempPath);

                        // optional: delete temp file after sending
                        System.IO.File.Delete(tempPath);
                    }
                }

                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Unable to update stock because the item was modified by someone else. Please review your cart and try again.");
                return View(order);
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while submitting the order. Please try again.");
                return View(order);
            }



            TempData["SuccessMessage"] = "Shipping info saved successfully.";

            return RedirectToAction("Details", "Order", new { id = order.Id });
        }
    }
}
