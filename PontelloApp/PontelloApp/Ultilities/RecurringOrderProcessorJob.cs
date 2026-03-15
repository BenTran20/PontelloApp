using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;
using PontelloApp.Services;

namespace PontelloApp.Ultilities
{
    public class RecurringOrderProcessorJob
    {
        private readonly PontelloAppContext _db;
        private readonly RecurringOrderService _recurringSvc;

        public RecurringOrderProcessorJob(
            PontelloAppContext db,
            RecurringOrderService recurringSvc)
        {
            _db = db;
            _recurringSvc = recurringSvc;
        }

        public async Task RunAsync()
        {
            var due = await _db.RecurringOrders
                .Where(r => r.IsActive && r.NextRun <= DateTime.Now)
                .ToListAsync();

            foreach (var r in due)
            {
                int newOrderId = 0;
                bool success = true;
                string msg = "";

                try
                {
                    newOrderId = await _recurringSvc.CreateOrderFromRecurring(r);
                    msg = "OK";
                }
                catch (Exception ex)
                {
                    success = false;
                    msg = ex.Message;
                }

                _db.RecurringOrderExecutionLogs.Add(new RecurringOrderExecutionLog
                {
                    RecurringOrderId = r.Id,
                    Success = success,
                    Message = msg,
                    NewOrderId = newOrderId > 0 ? newOrderId : null
                });

                r.NextRun = r.Frequency switch
                {
                    "Daily" => r.NextRun.AddDays(1),
                    "Weekly" => r.NextRun.AddDays(7),
                    "Monthly" => r.NextRun.AddMonths(1),
                    _ => r.NextRun.AddDays(1)
                };

                await _db.SaveChangesAsync();
            }
        }
    }
}