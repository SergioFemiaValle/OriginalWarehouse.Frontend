using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    public class CategoriaProductoController : Controller
    {
        private readonly ICategoriaProductoManager _categoriaProductoManager;
        private readonly ICompositeViewEngine _viewEngine;

        public CategoriaProductoController(ICategoriaProductoManager categoriaProductoManager, ICompositeViewEngine viewEngine)
        {
            _categoriaProductoManager = categoriaProductoManager;
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> IndexAsync(int page = 1, int pageSize = 10)
        {
            var categorias = await _categoriaProductoManager.ObtenerTodas();

            int totalRegistros = categorias.Count();

            // 🔹 Paginación
            var categoriasPaginadas = categorias
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(categoriasPaginadas);
        }

        [HttpGet]
        public IActionResult EditPartial(int? id)
        {
            CategoriaProducto categoria = id.HasValue && id > 0
                ? _categoriaProductoManager.ObtenerPorId(id.Value).Result
                : new CategoriaProducto();

            return PartialView("_EditCreatePartial", categoria);
        }

        [HttpPost]
        public async Task<IActionResult> Save(CategoriaProducto categoria)
        {
            ModelState.Remove(nameof(categoria.Productos));

            if (ModelState.IsValid)
            {
                if (categoria.Id == 0)
                {
                    await _categoriaProductoManager.Crear(categoria);
                }
                else
                {
                    var categoriaExistente = await _categoriaProductoManager.ObtenerPorId(categoria.Id);
                    if (categoriaExistente == null) return NotFound();

                    categoriaExistente.Nombre = categoria.Nombre;

                    await _categoriaProductoManager.Actualizar(categoriaExistente);
                }

                return Json(new { success = true, message = "Categoría guardada correctamente." });
            }

            var html = await RenderPartialViewToString("_EditCreatePartial", categoria);
            return Json(new { success = false, html });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var categoria = await _categoriaProductoManager.ObtenerPorId(id);
                if (categoria == null) return NotFound();

                await _categoriaProductoManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar la categoría. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

        #region private methods

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

        #endregion
    }
}