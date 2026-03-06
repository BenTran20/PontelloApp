using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

namespace PontelloApp.Services
{
    public class RecurringOrderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public RecurringOrderBackgroundService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PontelloAppContext>();
                var recurringSvc = scope.ServiceProvider.GetRequiredService<RecurringOrderService>();

                var due = await db.RecurringOrders
                    .Where(r => r.IsActive && r.NextRun <= DateTime.Now)
                    .ToListAsync(stoppingToken);

                foreach (var r in due)
                {
                    int newOrderId = 0;
                    bool success = true;
                    string msg = "";

                    try
                    {
                        newOrderId = await recurringSvc.CreateOrderFromRecurring(r);
                        msg = "OK";
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        msg = ex.Message;
                    }

                    db.RecurringOrderExecutionLogs.Add(new RecurringOrderExecutionLog
                    {
                        RecurringOrderId = r.Id,
                        Success = success,
                        Message = msg,
                        NewOrderId = newOrderId
                    });

                    r.NextRun = r.Frequency switch
                    {
                        "Daily" => r.NextRun.AddDays(1),
                        "Weekly" => r.NextRun.AddDays(7),
                        "Monthly" => r.NextRun.AddMonths(1),
                        _ => r.NextRun.AddDays(1)
                    };

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
        }
    }
}