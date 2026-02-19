using System.ComponentModel.DataAnnotations;

namespace PontelloApp.Models
{

    public class Vendor : Auditable, IValidatableObject
    {
        [Key]
        public int VendorID { get; set; }

        [Display(Name = "Vendor Name")]
        [Required(ErrorMessage = "Vendor Name is required.")]
        [StringLength(200, ErrorMessage = "Vendor Name cannot be more than 200 characters long.")]
        public string Name { get; set; } = "";

        [Display(Name = "Contact Name")]
        [StringLength(200, ErrorMessage = "Contact Name cannot be more than 200 characters long.")]
        public string? ContactName { get; set; }

        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit phone number (digits only).")]
        [StringLength(10)]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]

        public string? Email { get; set; }

        [Display(Name = "EIN")]
        [StringLength(50)]
        public string? EIN { get; set; }

        [Display(Name = "Is Tax Exempt")]
        public bool IsTaxExempt { get; set; } = false;


        public bool IsArchived { get; set; } = false;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
        public ICollection<Product>? Products { get; set; } = new HashSet<Product>();


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Rule 1: If IsTaxExempt = true, EIN is required
            if (IsTaxExempt && string.IsNullOrWhiteSpace(EIN))
            {
                yield return new ValidationResult(
                    "EIN is required when the vendor is tax exempt.",
                    new[] { nameof(EIN) });
            }

            // Rule 2: If EIN is provided, we can auto force IsTaxExempt or just warn
            if (!string.IsNullOrWhiteSpace(EIN) && !IsTaxExempt)
            {
                yield return new ValidationResult(
                    "Vendor has an EIN but Is Tax Exempt is not checked. " +
                    "Mark the vendor as tax exempt or remove the EIN.",
                    new[] { nameof(IsTaxExempt), nameof(EIN) });
            }
        }

    }

}
