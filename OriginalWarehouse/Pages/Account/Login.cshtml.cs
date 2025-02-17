using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OriginalWarehouse.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace OriginalWarehouse.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<Usuario> _signInManager;

        public LoginModel(SignInManager<Usuario> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        public class LoginInputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, false);

            if (result.Succeeded)
            {
                return RedirectToPage("/Index"); // 🔹 Redirige al dashboard principal
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Inicio de sesión inválido.");
                return Page();
            }
        }
    }
}
