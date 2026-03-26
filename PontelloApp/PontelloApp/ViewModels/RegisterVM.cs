using System.ComponentModel.DataAnnotations;

namespace PontelloApp.ViewModels
{
    public class RegisterVM
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        public string? BINorEIN { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(40, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 40 characters.")]
        [DataType(DataType.Password)]
        [Compare("ConfirmedPassword", ErrorMessage = "Passwords do not match.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmedPassword { get; set; }   
    }
}
