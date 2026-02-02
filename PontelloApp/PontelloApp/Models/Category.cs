using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PontelloApp.Models
{
    public class Category : IValidatableObject
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "The Category Name is required.")]
        [StringLength(100, ErrorMessage = "Category Name cannot be more than 100 characters long.")]
        public string Name { get; set; } = "";

        public ICollection<Product> Products { get; set; } = new HashSet<Product>();

        //self-reference
        public int? ParentCategoryID { get; set; }

        [ForeignKey("ParentCategoryID")]
        public Category? ParentCategory { get; set; }

        public ICollection<Category> SubCategories { get; set; } = new HashSet<Category>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Name.Length <= 2)
            {
                yield return new ValidationResult("Category Name must be at least 3 Characters long", new[] { "Name" });
            }
        }

    }
}
