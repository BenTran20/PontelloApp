using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PontelloApp.Models
{
    public class Shipping
    {
        public int ID { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        public string? TrackingNumber { get; set; }

        // navigation
        public int OrderId { get; set; }
        public Order? Order { get; set; }
    }
}
