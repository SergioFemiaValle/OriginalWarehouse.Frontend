using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    public class EstadoBultoController : Controller
    {
        private readonly IEstadoBultoManager _estadoBultoManager;
        private readonly ICompositeViewEngine _viewEngine;

        public EstadoBultoController(IEstadoBultoManager estadoBultoManager, ICompositeViewEngine viewEngine)
        {
            _estadoBultoManager = estadoBultoManager;
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var estados = await _estadoBultoManager.ObtenerTodos();

            int totalRegistros = estados.Count();

            // 🔹 Paginación
            var estadosPaginados = estados
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(estadosPaginados);
        }


        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            EstadoBulto estado = id.HasValue && id > 0
                ? await _estadoBultoManager.ObtenerPorId(id.Value)
                : new EstadoBulto();

            return PartialView("_EditCreatePartial", estado);
        }

        [HttpPost]
        public async Task<IActionResult> Save(EstadoBulto estado)
        {
            ModelState.Remove(nameof(estado.Bultos));

            if (ModelState.IsValid)
            {
                if (estado.Id == 0)
                {
                    await _estadoBultoManager.Crear(estado);
                }
                else
                {
                    var estadoExistente = await _estadoBultoManager.ObtenerPorId(estado.Id);
                    if (estadoExistente == null) return NotFound();

                    estadoExistente.Nombre = estado.Nombre;

                    await _estadoBultoManager.Actualizar(estadoExistente);
                }

                return Json(new { success = true, message = "Estado de bulto guardado correctamente." });
            }

            var html = await RenderPartialViewToString("_EditCreatePartial", estado);
            return Json(new { success = false, html });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var estado = await _estadoBultoManager.ObtenerPorId(id);
                if (estado == null) return NotFound();

                await _estadoBultoManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el estado de bulto. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

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
