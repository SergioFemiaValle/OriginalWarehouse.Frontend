using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador de autenticación y gestión de cuentas de usuario.
    /// Maneja el inicio y cierre de sesión en la aplicación.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;

        /// <summary>
        /// Constructor del controlador de cuentas.
        /// </summary>
        /// <param name="signInManager">Administrador de inicio de sesión de Identity.</param>
        /// <param name="userManager">Administrador de usuarios de Identity.</param>
        public AccountController(SignInManager<Usuario> signInManager, UserManager<Usuario> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
        /// Procesa la autenticación del usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="password">Contraseña del usuario.</param>
        /// <returns>Redirige al menú principal si el inicio de sesión es exitoso, 
        /// o muestra un mensaje de error si falla.</returns>
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);

            if (result.Succeeded)
                return RedirectToAction("Index", "MainMenu");

            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        /// <summary>
        /// Cierra la sesión del usuario actual y redirige a la vista de login.
        /// </summary>
        /// <returns>Redirección a la vista de inicio de sesión.</returns>
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
