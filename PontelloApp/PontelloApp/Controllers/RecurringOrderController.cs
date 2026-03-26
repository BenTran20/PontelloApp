using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using QuestPDF.Fluent;

namespace PontelloApp.Controllers
{
    public class RecurringOrderController : Controller
    {
        private readonly PontelloAppContext _context;

        public RecurringOrderController(PontelloAppContext context)
        {
            _context = context;
        }

        // GET: RecurringOrder
        public async Task<IActionResult> Index(int orderId)
        {
            var model = await _context.RecurringOrders
                .Where(r => r.OriginalOrderId == orderId)
                .ToListAsync();

            ViewBag.OrderId = orderId;
            return View(model);
        }

        // GET: RecurringOrder/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recurringOrder = await _context.RecurringOrders
                .Include(r => r.OriginalOrder)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recurringOrder == null)
            {
                return NotFound();
            }

            return View(recurringOrder);
        }

        // GET: RecurringOrder/Create
        public async Task<IActionResult> Create(int orderId)
        {
            var hasOrder = await _context.Orders.AnyAsync(o => o.Id == orderId);
            if (!hasOrder) return NotFound();

            var model = new RecurringOrder
            {
                OriginalOrderId = orderId,
                Frequency = "Daily",
                TimeOfDay = new TimeSpan(9, 0, 0)
            };
            return View(model);
        }

        // POST: RecurringOrder/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecurringOrder model)
        {
            if (!ModelState.IsValid) return View(model);

            model.NextRun = CalculateNextRun(model);
            _context.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Recurring order created.";
            RecurrMessage(model.OriginalOrderId, model);
            return RedirectToAction("Details", "Order", new { id = model.OriginalOrderId });
        }


        // GET: RecurringOrder/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recurringOrder = await _context.RecurringOrders.FindAsync(id);
            if (recurringOrder == null)
            {
                return NotFound();
            }
            ViewData["OriginalOrderId"] = new SelectList(_context.Orders, "Id", "Id", recurringOrder.OriginalOrderId);
            return View(recurringOrder);
        }

        // POST: RecurringOrder/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OriginalOrderId,Frequency,TimeOfDay,WeeklyDay,MonthlyDay,NextRun,IsActive")] RecurringOrder recurringOrder)
        {
            if (id != recurringOrder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recurringOrder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecurringOrderExists(recurringOrder.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            RecurrMessage(recurringOrder.OriginalOrderId, recurringOrder);
            ViewData["OriginalOrderId"] = new SelectList(_context.Orders, "Id", "Id", recurringOrder.OriginalOrderId);
            return View(recurringOrder);
        }

        // GET: RecurringOrder/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recurringOrder = await _context.RecurringOrders
                .Include(r => r.OriginalOrder)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recurringOrder == null)
            {
                return NotFound();
            }

            return View(recurringOrder);
        }

        // POST: RecurringOrder/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recurringOrder = await _context.RecurringOrders.FindAsync(id);
            if (recurringOrder != null)
            {
                _context.RecurringOrders.Remove(recurringOrder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Tracking
        public async Task<IActionResult> Tracking()
        {
            var model = await _context.RecurringOrders
                .Include(r => r.OriginalOrder)
                .ToListAsync();

            return View(model);
        }

        // Active
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, int orderId)
        {
            var r = await _context.RecurringOrders.FindAsync(id);
            if (r == null) return NotFound();

            r.IsActive = !r.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction("Tracking", new { orderId });
        }

        private bool RecurringOrderExists(int id)
        {
            return _context.RecurringOrders.Any(e => e.Id == id);
        }



        private DateTime CalculateNextRun(RecurringOrder r)
        {
            var now = DateTime.Now;
            var today = now.Date + r.TimeOfDay;

            if (r.Frequency == "Daily")
            {
                return today > now ? today : today.AddDays(1);
            }
            else if (r.Frequency == "Weekly")
            {
                int daysUntil = ((int)r.WeeklyDay!.Value - (int)now.DayOfWeek + 7) % 7;
                var next = now.Date.AddDays(daysUntil) + r.TimeOfDay;
                return next > now ? next : next.AddDays(7);
            }
            else if (r.Frequency == "Monthly")
            {
                var next = new DateTime(now.Year, now.Month, r.MonthlyDay!.Value)
                           .Add(r.TimeOfDay);
                if (next <= now) next = next.AddMonths(1);
                return next;
            }

            return now;
        }



        [HttpPost]
        public async Task<IActionResult> RecurrMessage(int id, RecurringOrder model)
        {
            var order = await _context.Orders
                .Include(o => o.Shipping)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.Shipping == null)
                order.Shipping = new Shipping();

            // Add shipping cost to total

            order.IsRecurringGenerated = true;

            // Recalculate totals including shipping
            var subtotal = order.Items?.Sum(i => i.TotalPrice) ?? 0m;

            // Keep existing tax amount (tax already calculated when shipping was first saved). If you need to recalc,
            // you can adopt the same BIN/EIN logic used elsewhere. Here we preserve order.TaxAmount.
            var tax = order.TaxAmount;

            order.TotalAmount = Math.Round(subtotal + tax + (order.Shipping?.ShippingCost ?? 0m), 2);

            await _context.SaveChangesAsync();

            //Time to send Order Message
            DateTime SendAt = DateTime.Now;

            //Time to send Recur Message
            DateTime SendAt1 = DateTime.Now;


            var now = DateTime.Now;
            var today = now.Date + model.TimeOfDay;

            //Daily
            TimeSpan Interval1 = TimeSpan.FromMinutes(3);
            TimeSpan Interval = TimeSpan.FromMinutes(6);

            // Generate PO PDF bytes
            byte[] pdfBytes = GeneratePurchaseOrderPdf(order);

            if (!string.IsNullOrWhiteSpace(order.Shipping?.Email))
            {
                string subject = $"Your Pontello Order {order.PONumber}";
                //original message
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

                //recurr message
                string body1 = $@"
                     <div style=""font-family: Arial, sans-serif; font-size: 14px; color: #333; text-align: left;"">

                     <p>Hi <strong>{order.Shipping.FullName}</strong>,</p>";

                if (model.Frequency == "Daily")
                {
                    body1 += $@"<p>Thank you for your order! This is a reminder that your order is recurred {model.Frequency}. You have 12 hours before being order is shipped.</p>";
                    //SendAt1 = DateTime.Now.AddHours(12);
                    //SendAt = DateTime.Now.AddDays(1).AddMinutes(model.TimeOfDay.TotalMinutes);
                    //Interval1 = TimeSpan.FromHours(12);
                    //Interval = TimeSpan.FromMinutes(model.TimeOfDay.TotalMinutes);
                    SendAt1 = DateTime.Now.AddMinutes(2);
                    SendAt = DateTime.Now.AddMinutes(4);
                }
                if (model.Frequency == "Weekly")
                {
                    body1 += $@"<p>Thank you for your order! This is a reminder that your order is recurred {model.Frequency}. You have 3 Days before being order is shipped.</p>";
                    //SendAt1 = DateTime.Now.AddDays(3);
                    //SendAt = DateTime.Now.AddMonths(1);
                    //Interval1 = TimeSpan.FromDays(3);
                    //int daysUntil = ((int)model.WeeklyDay!.Value - (int)now.DayOfWeek + 7) % 7;
                    //Interval = TimeSpan.FromDays(daysUntil);
                    SendAt1 = DateTime.Now.AddMinutes(2);
                    SendAt = DateTime.Now.AddMinutes(4);
                    Interval1 = TimeSpan.FromMinutes(3);
                    Interval = TimeSpan.FromMinutes(5);

                }
                if (model.Frequency == "Monthly")
                {
                    body1 += $@"<p>Thank you for your order! This is a reminder that your order is recurred {model.Frequency}. You have 15 Days before being order is shipped.</p>";
                    //TimeSpan Interval = TimeSpan.FromDays(model.MonthlyDay.Value); 
                    //SendAt1 = DateTime.Now.AddDays(15);
                    //SendAt = DateTime.Now.AddMonths(1);
                    // int WhenToSend = model.MonthlyDay.Value / 2;
                    //Interval1 = TimeSpan.FromDays(WhenToSend);
                    SendAt1 = DateTime.Now.AddMinutes(3);
                    SendAt = DateTime.Now.AddMinutes(5);
                    Interval1 = TimeSpan.FromMinutes(3);
                    Interval = TimeSpan.FromMinutes(5); //unsure precise day each month
                }

                body1 += @$"<p>You can find your Purchase Order attached for your reference.</p>

                     <hr style=""border:none; border-top:1px solid #eee; margin:20px 0;"" />

                     <p style=""font-size:12px; color:#777;"">
                         Pontello Team<br/>
                         Questions? Reply to this email 
                     </p>
                 </div>";

                // Save pdf temporarily
                // Generate PDF into temp file
                string tempPath = Path.Combine(Path.GetTempPath(), $"PO_{order.PONumber}.pdf");

                try
                {

                    // IMPORTANT: use System.IO.File
                    System.IO.File.WriteAllBytes(tempPath, pdfBytes);

                    //Recurr Message
                    var schedule1 = new ScheduledEmail
                    {
                        Email = order.Shipping.Email,
                        Subject = subject,
                        HtmlBody = body1,
                        AttachmentBytes = pdfBytes,
                        AttachmentName = tempPath,
                        NextSendAt = SendAt1,
                        RepeatInterval = Interval1,
                        IsActive = model.IsActive
                    };

                    //Order Message
                    var schedule = new ScheduledEmail
                    {
                        Email = order.Shipping.Email,
                        Subject = subject,
                        HtmlBody = body,
                        AttachmentBytes = pdfBytes,
                        AttachmentName = tempPath,
                        NextSendAt = SendAt,
                        //NextSendAt = CalculateNextRun(model), //unsure why the following line doesnt work correctly, test if same issue
                        RepeatInterval = Interval,
                        IsActive = model.IsActive
                    };

                    _context.ScheduledEmails.Add(schedule1);
                    _context.ScheduledEmails.Add(schedule);
                    await _context.SaveChangesAsync();


                }
                finally
                {
                    // optional: delete temp file after sending
                    try { System.IO.File.Delete(tempPath); } catch { /* swallow */ }
                }
            }

            return RedirectToAction("Index", "Order");
        }

        private byte[] GeneratePurchaseOrderPdf(Order order)
        {
            var items = order.Items.Select(i => new
            {
                Product = i.Product?.ProductName ?? "",
                Quantity = i.Quantity,
                Price = i.UnitPrice,
                Total = i.Quantity * i.UnitPrice
            }).ToList();

            decimal subtotal = items.Sum(i => i.Total);
            decimal tax = order.TaxAmount;
            decimal shippingCost = order.Shipping?.ShippingCost ?? 0m;
            decimal grandTotal = order.TotalAmount;

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // HEADER
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

                    // CONTENT
                    page.Content().PaddingVertical(15).Column(col =>
                    {

                        // SHIPPING INFO
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Ship To").Bold();
                                c.Item().Text(order.Shipping?.FullName ?? "");
                                c.Item().Text(order.Shipping?.FullAddress ?? "N/A");
                                c.Item().Text(order.Shipping?.Email ?? "");
                                c.Item().Text(order.Shipping?.Phone ?? "");

                                if (!string.IsNullOrWhiteSpace(order.Shipping?.BinOrEin))
                                    c.Item().Text($"BIN: {order.Shipping.BinOrEin}");

                                if (!string.IsNullOrWhiteSpace(order.Shipping?.TrackingNumber))
                                    c.Item().Text($"Tracking #: {order.Shipping.TrackingNumber}");
                            });
                        });

                        col.Item().PaddingVertical(10);

                        // TABLE
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

                        col.Item().PaddingTop(15);

                        // TOTALS
                        col.Item().AlignRight().Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Subtotal:");
                                r.ConstantItem(100).AlignRight().Text("$" + subtotal.ToString("0.00"));
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Tax:");
                                r.ConstantItem(100).AlignRight().Text("$" + tax.ToString("0.00"));
                            });

                            if (shippingCost > 0)
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().AlignRight().Text("Shipping:");
                                    r.ConstantItem(100).AlignRight().Text("$" + shippingCost.ToString("0.00"));
                                });
                            }

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Total:").Bold();
                                r.ConstantItem(100).AlignRight().Text("$" + grandTotal.ToString("0.00")).Bold();
                            });
                        });
                    });

                    // FOOTER
                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(10)
                        .FontColor("#777777");
                });

            }).GeneratePdf();

            return pdf;
        }
    }
}
