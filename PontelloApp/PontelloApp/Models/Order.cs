using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PontelloApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string PONumber { get; set; } = "";

        public int RevisionNumber { get; set; } = 0;

        public int DealerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Draft;

        public ICollection<OrderItem>? Items { get; set; } = new HashSet<OrderItem>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
