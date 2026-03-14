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
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.Id == r.OriginalOrderId);

            if (original == null) throw new Exception("Original order not found");

            var newOrder = new Order
            {
                PONumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}",
                DealerId = original.DealerId,
                Status = OrderStatus.Submitted,
                CreatedAt = DateTime.Now
            };
            if (original.Shipping != null)
            {
                newOrder.Shipping = new Shipping
                {
                    FullName = original.Shipping.FullName,
                    StreetAddress = original.Shipping.StreetAddress,
                    City=original.Shipping.City,
                    Province = original.Shipping.Province,
                    Country = original.Shipping.Country,
                    PostalCode = original.Shipping.PostalCode,
                    Phone = original.Shipping.Phone,
                    Email = original.Shipping.Email,
                    BinOrEin = original.Shipping.BinOrEin
                };
            }


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
