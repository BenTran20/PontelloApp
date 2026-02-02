using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace PontelloApp.Models
{
    public class Variant : IValidatableObject
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }   

        [StringLength(100)]
        public string Value { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value.Length <= 2)
            {
                yield return new ValidationResult("Variant Name must be at least 3 Characters long", new[] { "Value" });
            }
        }
    }
}
