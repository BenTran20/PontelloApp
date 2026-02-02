using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PontelloApp.Models
{
    public class Product : IValidatableObject
    {
        public int ID { get; set; }

        [Display(Name ="Name")]
        [Required(ErrorMessage = "Product Name is required.")]
        [StringLength(200, ErrorMessage = "Product Name cannot be more than 200 characters long.")]
        public string ProductName { get; set; } = "";

        [StringLength(200, ErrorMessage = "Product Description cannot be more than 200 characters long.")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public int? CategoryID { get; set; }

        public Category? Category { get; set; }

        public ICollection<ProductVariant>? Variants { get; set; } = new HashSet<ProductVariant>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ProductName.Length <= 2 || ProductName.IsNullOrEmpty())
            {
                yield return new ValidationResult("Product Name must be at least 3 Characters long", new[] { "ProductName" });
            }
        }

    }
}
