using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using mvc.Data;
using mvc.Models;
using BCrypt.Net;

namespace mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        public IActionResult Register(string name, string email, string password, string confirmPassword)
        {
            // Basic validation
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            // Check if email already exists
            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Email already registered.";
                return View();
            }

            // Hash password and create user
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var memberRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Member");
            var user = new User
            {
                Name = name,
                Email = email,
                Password = hashedPassword,
                RoleId = memberRole?.Id ?? 1, // Fallback to 1 if Member role not found
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Store user ID in session for verification
            HttpContext.Session.SetInt32("PendingUserId", user.Id);
            
            return RedirectToAction("VerifyEmail");
        }

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null)
            {
                return RedirectToAction("Register");
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Register");
            }

            ViewBag.UserEmail = user.Email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmailConfirm()
        {
            var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null)
            {
                return RedirectToAction("Register");
            }

            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.Now;
                _context.SaveChanges();

                // Log the user in by creating authentication cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Member"),
                    new Claim("EmailVerified", "true")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false
                };

                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);
                HttpContext.Session.Remove("PendingUserId");
                
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Register");
        }

        public async Task<IActionResult> Logout() 
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }

    }
}
