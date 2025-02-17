using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    public class AlmacenamientoEspecialController : Controller
    {
        private readonly IAlmacenamientoEspecialManager _almacenamientoEspecialManager;
        private readonly ICompositeViewEngine _viewEngine;

        public AlmacenamientoEspecialController(IAlmacenamientoEspecialManager almacenamientoEspecialManager, ICompositeViewEngine viewEngine)
        {
            _almacenamientoEspecialManager = almacenamientoEspecialManager;
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var almacenamientos = await _almacenamientoEspecialManager.ObtenerTodos();

            int totalRegistros = almacenamientos.Count();

            // 🔹 Paginación
            var almacenamientosPaginados = almacenamientos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(almacenamientosPaginados);
        }

        [HttpGet]
        public IActionResult EditPartial(int? id)
        {
            AlmacenamientoEspecial almacenamiento = id.HasValue && id > 0
                ? _almacenamientoEspecialManager.ObtenerPorId(id.Value).Result
                : new AlmacenamientoEspecial();

            return PartialView("_EditCreatePartial", almacenamiento);
        }

        [HttpPost]
        public async Task<IActionResult> Save(AlmacenamientoEspecial almacenamiento)
        {
            ModelState.Remove(nameof(almacenamiento.Productos));

            if (ModelState.IsValid)
            {
                if (almacenamiento.Id == 0)
                {
                    await _almacenamientoEspecialManager.Crear(almacenamiento);
                }
                else
                {
                    var almacenamientoExistente = await _almacenamientoEspecialManager.ObtenerPorId(almacenamiento.Id);
                    if (almacenamientoExistente == null) return NotFound();

                    almacenamientoExistente.Nombre = almacenamiento.Nombre;

                    await _almacenamientoEspecialManager.Actualizar(almacenamientoExistente);
                }

                return Json(new { success = true, message = "Almacenamiento especial guardado correctamente." });
            }

            var html = await RenderPartialViewToString("_EditCreatePartial", almacenamiento);
            return Json(new { success = false, html });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var almacenamiento = await _almacenamientoEspecialManager.ObtenerPorId(id);
                if (almacenamiento == null) return NotFound();

                await _almacenamientoEspecialManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el almacenamiento especial. Intente nuevamente.";
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