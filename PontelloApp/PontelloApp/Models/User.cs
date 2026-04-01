using Microsoft.AspNetCore.Identity;
using Org.BouncyCastle.Bcpg;

namespace PontelloApp.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }
        public string? BINorEIN { get; set; }

        public AccountStatus Status { get; set; } = AccountStatus.Pending;

        public ICollection<Order>? Orders { get; set; } = new HashSet<Order>();

    }
}
