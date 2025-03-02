using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador para la gestión de roles de usuario.
    /// Permite listar, crear, editar y eliminar roles en el sistema.
    /// </summary>
    public class RolController : Controller
    {
        private readonly RoleManager<Rol> _roleManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de roles.
        /// </summary>
        public RolController(RoleManager<Rol> roleManager, ICompositeViewEngine viewEngine)
        {
            _roleManager = roleManager;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// Muestra la lista de roles disponibles en el sistema.
        /// </summary>
        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        /// <summary>
        /// Carga la vista parcial para la creación o edición de un rol.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            Rol? rol = id.HasValue
                ? await _roleManager.FindByIdAsync(id.Value.ToString()) // ID en Identity es un string
                : new Rol();

            return PartialView("_EditCreatePartial", rol);
        }

        /// <summary>
        /// Guarda un nuevo rol o actualiza uno existente.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Rol rol)
        {
            if (ModelState.IsValid)
            {
                if (rol.Id == 0)
                {
                    // Crear nuevo rol
                    var resultado = await _roleManager.CreateAsync(rol);
                    if (!resultado.Succeeded)
                    {
                        return Json(new { success = false, message = "Error al crear el rol." });
                    }
                }
                else
                {
                    // Actualizar rol existente
                    var rolExistente = await _roleManager.FindByIdAsync(rol.Id.ToString());
                    if (rolExistente == null) return NotFound();

                    rolExistente.Name = rol.Name; // `Name` en Identity es el nombre del rol

                    var resultado = await _roleManager.UpdateAsync(rolExistente);
                    if (!resultado.Succeeded)
                    {
                        return Json(new { success = false, message = "Error al actualizar el rol." });
                    }
                }

                return Json(new { success = true, message = "Rol guardado correctamente." });
            }

            var html = await RenderPartialViewToString("_EditCreatePartial", rol);
            return Json(new { success = false, html });
        }

        /// <summary>
        /// Elimina un rol existente.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var rol = await _roleManager.FindByIdAsync(id.ToString());
            if (rol == null) return NotFound();

            var resultado = await _roleManager.DeleteAsync(rol);
            if (!resultado.Succeeded)
            {
                return Json(new { success = false, message = "Error al eliminar el rol." });
            }

            return Json(new { success = true, message = "Rol eliminado correctamente." });
        }

        /// <summary>
        /// Renderiza una vista parcial como string.
        /// </summary>
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
