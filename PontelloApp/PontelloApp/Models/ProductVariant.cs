using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PontelloApp.Models
{
    public class ProductVariant : IValidatableObject
    {
        public int Id { get; set; }

        public List<Variant> Options { get; set; } = new List<Variant>();


        [Required(ErrorMessage = "Unit price is required.")]
        [Display(Name = "Unit Price")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        [StringLength(50, ErrorMessage = "SKU_ExternalID cannot be more than 100 characters long.")]
        [Display(Name = "SKU")]
        public string? SKU_ExternalID { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UnitPrice <= 0)
            {
                yield return new ValidationResult("Unit Price cannot be less than or equal to $0", new[] { "UnitPrice" });
            }
        }
    }
}
