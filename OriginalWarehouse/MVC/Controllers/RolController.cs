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
    public class RolController : Controller
    {
        private readonly RoleManager<Rol> _roleManager;
        private readonly ICompositeViewEngine _viewEngine;

        public RolController(RoleManager<Rol> roleManager, ICompositeViewEngine viewEngine)
        {
            _roleManager = roleManager;
            _viewEngine = viewEngine;
        }

        // ✅ LISTAR ROLES
        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        // ✅ CARGAR VISTA PARCIAL PARA CREAR/EDITAR
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            Rol? rol = id.HasValue
                ? await _roleManager.FindByIdAsync(id.Value.ToString()) // 🛠️ CORRECCIÓN: ID ahora es string
                : new Rol();

            return PartialView("_EditCreatePartial", rol);
        }

        // ✅ GUARDAR (CREAR O EDITAR) ROL
        [HttpPost]
        public async Task<IActionResult> Save(Rol rol)
        {
            //if (ModelState.IsValid)
            //{
                if (rol.Id == 0)
                {
                    // CREAR NUEVO ROL
                    var resultado = await _roleManager.CreateAsync(rol);
                    if (!resultado.Succeeded)
                    {
                        return Json(new { success = false, message = "Error al crear el rol." });
                    }
                }
                else
                {
                    // ACTUALIZAR ROL EXISTENTE
                    var rolExistente = await _roleManager.FindByIdAsync(rol.Id.ToString());
                    if (rolExistente == null) return NotFound();

                    rolExistente.Name = rol.Name; // 🛠️ `Name` en Identity es el nombre del rol

                    var resultado = await _roleManager.UpdateAsync(rolExistente);
                    if (!resultado.Succeeded)
                    {
                        return Json(new { success = false, message = "Error al actualizar el rol." });
                    }
                }

                return Json(new { success = true, message = "Rol guardado correctamente." });
            //}

            //var html = await RenderPartialViewToString("_EditCreatePartial", rol);
            //return Json(new { success = false, html });
        }

        // ✅ ELIMINAR ROL
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
