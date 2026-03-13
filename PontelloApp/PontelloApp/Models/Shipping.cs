using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PontelloApp.Models
{
    public class Shipping
    {
        public int ID { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        // Optional BIN or EIN. If provided, customer will be tax-exempt for the order.
        [Display(Name = "BIN / EIN")]
        public string? BinOrEin { get; set; }

        public string? TrackingNumber { get; set; }

        public decimal ShippingCost { get; set; }

        // navigation
        public int OrderId { get; set; }
        public Order? Order { get; set; }
    }
}
