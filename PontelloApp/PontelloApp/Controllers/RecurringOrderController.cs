using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

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

    }
}
