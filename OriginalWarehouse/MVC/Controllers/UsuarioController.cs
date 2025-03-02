using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador para la gestión de usuarios dentro del sistema.
    /// </summary>
    public class UsuarioController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<Rol> _roleManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de usuarios.
        /// </summary>
        /// <param name="userManager">Gestor de usuarios.</param>
        /// <param name="roleManager">Gestor de roles.</param>
        /// <param name="viewEngine">Motor de vistas.</param>
        public UsuarioController(UserManager<Usuario> userManager, RoleManager<Rol> roleManager, ICompositeViewEngine viewEngine)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// Obtiene la lista de usuarios y sus respectivos roles.
        /// </summary>
        /// <returns>Vista con la lista de usuarios.</returns>
        public async Task<IActionResult> Index()
        {
            var usuarios = _userManager.Users.ToList();
            var userRoles = new Dictionary<int, string>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                userRoles[usuario.Id] = roles.FirstOrDefault() ?? "Sin rol";
            }

            ViewBag.UserRoles = userRoles;
            return View(usuarios);
        }

        /// <summary>
        /// Carga la vista parcial para crear o editar un usuario.
        /// </summary>
        /// <param name="id">Identificador del usuario.</param>
        /// <returns>Vista parcial de edición/creación de usuario.</returns>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Usuario usuario = id.HasValue
                ? await _userManager.FindByIdAsync(id.Value.ToString()) ?? new Usuario()
                : new Usuario();

            var roles = await _userManager.GetRolesAsync(usuario);
            ViewBag.RolSeleccionado = roles.FirstOrDefault();

            return PartialView("_EditCreatePartial", usuario);
        }

        /// <summary>
        /// Guarda un usuario nuevo o actualiza uno existente.
        /// </summary>
        /// <param name="Id">Identificador del usuario.</param>
        /// <param name="UserName">Nombre de usuario.</param>
        /// <param name="Email">Correo electrónico del usuario.</param>
        /// <param name="Password">Contraseña del usuario.</param>
        /// <param name="RolId">Identificador del rol asignado.</param>
        /// <returns>JSON con el estado de la operación.</returns>
        [HttpPost]
        public async Task<IActionResult> Save(int? Id, string UserName, string Email, string Password, int? RolId)
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Email))
            {
                return Json(new { success = false, message = "Nombre de usuario y correo son obligatorios." });
            }

            string RolSeleccionado = null;
            if (RolId.HasValue)
            {
                var rol = await _roleManager.FindByIdAsync(RolId.ToString());
                if (rol != null)
                {
                    RolSeleccionado = rol.Name;
                }
            }

            if (Id == 0 || Id == null)
            {
                if (string.IsNullOrEmpty(Password))
                {
                    return Json(new { success = false, message = "Debe especificar una contraseña para el nuevo usuario." });
                }

                var nuevoUsuario = new Usuario
                {
                    UserName = UserName,
                    Email = Email,
                    EmailConfirmed = true,
                    Nombre = UserName
                };

                var resultado = await _userManager.CreateAsync(nuevoUsuario, Password);
                if (!resultado.Succeeded)
                {
                    return Json(new { success = false, message = "Error al crear el usuario.", errors = resultado.Errors });
                }

                if (!string.IsNullOrEmpty(RolSeleccionado))
                {
                    await _userManager.AddToRoleAsync(nuevoUsuario, RolSeleccionado);
                }
            }
            else
            {
                var usuarioExistente = await _userManager.FindByIdAsync(Id.ToString());
                if (usuarioExistente == null) return NotFound();

                usuarioExistente.UserName = UserName;
                usuarioExistente.Email = Email;

                var resultado = await _userManager.UpdateAsync(usuarioExistente);
                if (!resultado.Succeeded)
                {
                    return Json(new { success = false, message = "Error al actualizar el usuario.", errors = resultado.Errors });
                }

                var rolesActuales = await _userManager.GetRolesAsync(usuarioExistente);
                await _userManager.RemoveFromRolesAsync(usuarioExistente, rolesActuales);
                if (!string.IsNullOrEmpty(RolSeleccionado))
                {
                    await _userManager.AddToRoleAsync(usuarioExistente, RolSeleccionado);
                }

                if (!string.IsNullOrEmpty(Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(usuarioExistente);
                    var cambioPass = await _userManager.ResetPasswordAsync(usuarioExistente, token, Password);
                    if (!cambioPass.Succeeded)
                    {
                        return Json(new { success = false, message = "Error al actualizar la contraseña.", errors = cambioPass.Errors });
                    }
                }
            }

            return Json(new { success = true, message = "Usuario guardado correctamente." });
        }

        /// <summary>
        /// Elimina un usuario del sistema.
        /// </summary>
        /// <param name="id">Identificador del usuario a eliminar.</param>
        /// <returns>JSON con el estado de la operación.</returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario == null) return NotFound();

            var resultado = await _userManager.DeleteAsync(usuario);
            if (!resultado.Succeeded)
            {
                return Json(new { success = false, message = "Error al eliminar el usuario." });
            }

            return Json(new { success = true, message = "Usuario eliminado correctamente." });
        }

        /// <summary>
        /// Carga la lista de roles para asignación en la vista.
        /// </summary>
        private async Task CargarListas()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();
        }

        /// <summary>
        /// Renderiza una vista parcial como cadena de texto.
        /// </summary>
        /// <param name="viewName">Nombre de la vista parcial.</param>
        /// <param name="model">Modelo a renderizar.</param>
        /// <returns>Vista parcial renderizada como cadena.</returns>
        private async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);
                if (!viewResult.Success)
                {
                    throw new InvalidOperationException($"No se encontró la vista: {viewName}");
                }

                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }
    }
}
