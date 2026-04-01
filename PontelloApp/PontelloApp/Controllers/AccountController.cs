using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PontelloApp.Models;
using PontelloApp.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace PontelloApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                // Find user first to check pending/active status
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Email or Password is incorrect.");
                    return View(model);
                }

                if (user.Status == AccountStatus.Pending)
                {
                    ModelState.AddModelError("", "Your account is pending admin approval. You cannot sign in yet.");
                    return View(model);
                }

                if (user.Status == AccountStatus.Inactive)
                {
                    ModelState.AddModelError("", "Your account has been deactivated. You cannot sign in.");
                    return View(model);
                }

                // Use UserName for sign-in (UserName is Email at registration)
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email or Password is incorrect.");
                    return View(model);
                }
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                User user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.Phone,
                    Email = model.Email,
                    BINorEIN = model.BINorEIN,
                    UserName = model.Email,
                    // New accounts are pending and not active until admin approves
                    Status = AccountStatus.Pending
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign newly registered users to the "Dealer" role by default.
                    await _userManager.AddToRoleAsync(user, "Dealer");

                    TempData["Message"] = "Registration successful. Your account is pending admin approval.";
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            return View(model);

        }

        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Something is wrong !!");
                    return View(model);
                }
                else
                {
                    return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
                }
            }
            return View(model);
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordVM { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)

        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email not Found.");
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Something went wrong !!");
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Accounts()
        {
            // All users (model)
            var users = _userManager.Users.ToList();

            // Pending users (for the Pending tab) — use a separate list via query
            var pendingUsers = _userManager.Users.Where(u => u.Status == AccountStatus.Pending).ToList();

            // Roles dictionary (existing behavior)
            var rolesDict = new Dictionary<string, string>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                rolesDict[u.Id] = string.Join(", ", roles);
            }

            ViewBag.UserRoles = rolesDict;
            ViewBag.PendingUsers = pendingUsers;

            return View(users);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRoles = roles;

            // Pass enum values for dropdown
            ViewBag.AllStatuses = Enum.GetValues(typeof(AccountStatus))
                                      .Cast<AccountStatus>()
                                      .ToList();

            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(
            string id,
            string firstName,
            string lastName,
            string email,
            string phone,
            string binOrEIN,
            AccountStatus status,
            List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var seedAdminEmail = "admin@gmail.com";
            if (user.Email == seedAdminEmail && !roles.Contains("Admin"))
            {
                ModelState.AddModelError("", "Seed admin must always have the Admin role.");
                ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
                ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
                ViewBag.AllStatuses = Enum.GetValues(typeof(AccountStatus)).Cast<AccountStatus>().ToList();
                return View(user);
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.PhoneNumber = phone;
            user.BINorEIN = binOrEIN;
            user.Status = status; 

            // roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = roles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(roles).ToList();

            if (rolesToAdd.Any())
                await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (rolesToRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            await _userManager.UpdateAsync(user);
            TempData["Message"] = "User updated successfully!";
            return RedirectToAction("EditUser", new { id = user.Id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("DeactivateUser")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Status = AccountStatus.Inactive;
            await _userManager.UpdateAsync(user);

            TempData["Message"] = $"User {user.Email} deactivated successfully.";
            return RedirectToAction("Accounts");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("ActivateUser")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Status = AccountStatus.Active;
            await _userManager.UpdateAsync(user);

            TempData["Message"] = $"User {user.Email} activated successfully.";
            return RedirectToAction("Accounts");
        }

        [Authorize]
        public IActionResult Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userManager.FindByIdAsync(userId).Result;

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [Authorize]
        public IActionResult Settings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userManager.FindByIdAsync(userId).Result;

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string FirstName, string LastName, string Email, string PhoneNumber)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = result.Succeeded ? "Profile updated successfully!" : "Update failed.";

            return RedirectToAction("Settings");
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePendingConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Status = AccountStatus.Active;
            var result = await _userManager.UpdateAsync(user);

            TempData["Message"] = result.Succeeded
                ? $"User {user.Email} approved and activated."
                : $"Unable to approve user {user.Email}.";

            return RedirectToAction("Accounts");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPendingConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Status = AccountStatus.Rejected;
            var result = await _userManager.UpdateAsync(user);

            TempData["Message"] = result.Succeeded
                ? $"User {user.Email} rejected."
                : $"Unable to reject user {user.Email}.";

            return RedirectToAction("Accounts");
        }
    }
}
