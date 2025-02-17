using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Domain.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<Rol> _roleManager;
        private readonly ICompositeViewEngine _viewEngine;

        public UsuarioController(UserManager<Usuario> userManager, RoleManager<Rol> roleManager, ICompositeViewEngine viewEngine)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _viewEngine = viewEngine;
        }

        // ✅ LISTAR USUARIOS
        public async Task<IActionResult> Index()
        {
            var usuarios = _userManager.Users.ToList(); // Obtener todos los usuarios
            var userRoles = new Dictionary<int, string>(); // Diccionario para almacenar usuarioId -> Nombre del Rol

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                userRoles[usuario.Id] = roles.FirstOrDefault() ?? "Sin rol"; // Tomar el primer rol o asignar "Sin rol"
            }

            ViewBag.UserRoles = userRoles; // Pasar los roles a la vista

            return View(usuarios);
        }

        // ✅ CARGAR VISTA PARCIAL PARA CREAR O EDITAR
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

        // ✅ GUARDAR USUARIO (CREAR O ACTUALIZAR)
        [HttpPost]
        public async Task<IActionResult> Save(int? Id, string UserName, string Email, string Password, int? RolId)
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Email))
            {
                return Json(new { success = false, message = "Nombre de usuario y correo son obligatorios." });
            }

            // 🔹 Obtener el nombre del rol a partir del ID
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
                // 🔹 CREAR NUEVO USUARIO
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

                // 🔹 ASIGNAR ROL SI SELECCIONADO
                if (!string.IsNullOrEmpty(RolSeleccionado))
                {
                    await _userManager.AddToRoleAsync(nuevoUsuario, RolSeleccionado);
                }
            }
            else
            {
                // 🔹 ACTUALIZACIÓN DE USUARIO EXISTENTE
                var usuarioExistente = await _userManager.FindByIdAsync(Id.ToString());
                if (usuarioExistente == null) return NotFound();

                usuarioExistente.UserName = UserName;
                usuarioExistente.Email = Email;

                var resultado = await _userManager.UpdateAsync(usuarioExistente);
                if (!resultado.Succeeded)
                {
                    return Json(new { success = false, message = "Error al actualizar el usuario.", errors = resultado.Errors });
                }

                // 🔹 ACTUALIZAR ROL
                var rolesActuales = await _userManager.GetRolesAsync(usuarioExistente);
                await _userManager.RemoveFromRolesAsync(usuarioExistente, rolesActuales);
                if (!string.IsNullOrEmpty(RolSeleccionado))
                {
                    await _userManager.AddToRoleAsync(usuarioExistente, RolSeleccionado);
                }

                // 🔹 ACTUALIZAR CONTRASEÑA SI SE INGRESA UNA NUEVA
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


        // ✅ ELIMINAR USUARIO
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

        // ✅ CARGAR LISTA DE ROLES
        private async Task CargarListas()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();
        }

        // ✅ RENDERIZAR VISTAS PARCIALES
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
