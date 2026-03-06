using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

namespace PontelloApp.Services
{
    public class RecurringOrderService
    {
        private readonly PontelloAppContext _db;

        public RecurringOrderService(PontelloAppContext db)
        {
            _db = db;
        }

        public async Task<int> CreateOrderFromRecurring(RecurringOrder r)
        {
            var original = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == r.OriginalOrderId);

            if (original == null) throw new Exception("Original order not found");

            var newOrder = new Order
            {
                DealerId = original.DealerId,
                Status = OrderStatus.Draft,
                CreatedAt = DateTime.Now
            };

            foreach (var item in original.Items)
            {
                newOrder.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            newOrder.TotalAmount = newOrder.Items.Sum(x => x.TotalPrice);

            _db.Orders.Add(newOrder);
            await _db.SaveChangesAsync();

            return newOrder.Id;
        }
    }
}