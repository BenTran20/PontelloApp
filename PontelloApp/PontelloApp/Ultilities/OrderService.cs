using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

namespace PontelloApp.Ultilities
{
    public class OrderService
    {
        private readonly PontelloAppContext _db;

        public OrderService(PontelloAppContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lấy hoặc tạo Order draft cho dealer hiện tại
        /// </summary>
        public async Task<Order> GetOrCreateDraftOrderAsync(int dealerId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.DealerId == dealerId && o.Status == OrderStatus.Draft);

            if (order == null)
            {
                order = new Order
                {
                    DealerId = dealerId,
                    Status = OrderStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
            }

            return order;
        }

        /// <summary>
        /// Thêm sản phẩm / variant + quantity vào order draft
        /// </summary>
        public async Task AddToCartAsync(int dealerId, int productId, int? variantId, int quantity)
        {
            var order = await GetOrCreateDraftOrderAsync(dealerId);

            // Lấy variant nếu có
            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = await _db.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == variantId.Value);
            }

            var product = await _db.Products.FindAsync(productId)
                          ?? throw new Exception("Product not found");

            decimal unitPrice = variant.UnitPrice;

            var existingItem = order.Items.FirstOrDefault(i =>
                i.ProductId == productId && i.ProductVariantId == variantId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var orderItem = new OrderItem
                {
                    ProductId = productId,
                    ProductVariantId = variantId,
                    Quantity = quantity,
                    UnitPrice = unitPrice
                };
                order.Items.Add(orderItem);
            }

            // Cập nhật tổng tiền order
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// </summary>
        public async Task<List<OrderItem>> GetCartItemsAsync(int dealerId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.DealerId == dealerId && o.Status == OrderStatus.Draft);

            return order?.Items.ToList() ?? new List<OrderItem>();
        }

        /// <summary>
        /// </summary>
        public async Task UpdateOrderTotalAsync(int dealerId)
        {
            var order = await GetOrCreateDraftOrderAsync(dealerId);
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
            await _db.SaveChangesAsync();
        }

    }
}
