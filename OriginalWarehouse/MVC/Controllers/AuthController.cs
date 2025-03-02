using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.MVC.Models;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador de autenticación de usuarios.
    /// Maneja el inicio de sesión, registro y cierre de sesión.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        /// <summary>
        /// Constructor del controlador de autenticación.
        /// </summary>
        /// <param name="userManager">Gestor de usuarios de Identity.</param>
        /// <param name="signInManager">Gestor de inicio de sesión de Identity.</param>
        public AuthController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Muestra la vista de inicio de sesión.
        /// </summary>
        /// <returns>Vista de login.</returns>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Procesa el inicio de sesión del usuario.
        /// </summary>
        /// <param name="model">Modelo con los datos de inicio de sesión.</param>
        /// <returns>Redirige a la página principal si el inicio de sesión es exitoso, 
        /// o muestra un mensaje de error si falla.</returns>
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            }
            return View(model);
        }

        /// <summary>
        /// Muestra la vista de registro de usuario.
        /// </summary>
        /// <returns>Vista de registro.</returns>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Procesa el registro de un nuevo usuario.
        /// </summary>
        /// <param name="model">Modelo con los datos del nuevo usuario.</param>
        /// <returns>Redirige a la página principal si el registro es exitoso, 
        /// o muestra un mensaje de error si falla.</returns>
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Usuario { UserName = model.Email, Email = model.Email/*, Nombre = model.Nombre*/ };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        /// <summary>
        /// Cierra la sesión del usuario actual y redirige a la vista de login.
        /// </summary>
        /// <returns>Redirección a la vista de inicio de sesión.</returns>
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
