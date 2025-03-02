using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador para la gestión de categorías de productos.
    /// Permite listar, crear, editar y eliminar categorías.
    /// </summary>
    public class CategoriaProductoController : Controller
    {
        private readonly ICategoriaProductoManager _categoriaProductoManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de categorías de productos.
        /// </summary>
        /// <param name="categoriaProductoManager">Gestor de categorías de productos.</param>
        /// <param name="viewEngine">Motor de renderizado de vistas parciales.</param>
        public CategoriaProductoController(ICategoriaProductoManager categoriaProductoManager, ICompositeViewEngine viewEngine)
        {
            _categoriaProductoManager = categoriaProductoManager;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// Muestra la lista de categorías de productos con paginación.
        /// </summary>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Cantidad de registros por página.</param>
        /// <returns>Vista con la lista de categorías paginadas.</returns>
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

        /// <summary>
        /// Muestra la vista parcial para la creación o edición de una categoría de producto.
        /// </summary>
        /// <param name="id">ID de la categoría (opcional).</param>
        /// <returns>Vista parcial con el formulario de edición o creación.</returns>
        [HttpGet]
        public IActionResult EditPartial(int? id)
        {
            CategoriaProducto categoria = id.HasValue && id > 0
                ? _categoriaProductoManager.ObtenerPorId(id.Value).Result
                : new CategoriaProducto();

            return PartialView("_EditCreatePartial", categoria);
        }

        /// <summary>
        /// Guarda una nueva categoría de producto o actualiza una existente.
        /// </summary>
        /// <param name="categoria">Objeto de categoría de producto a guardar.</param>
        /// <returns>Json con el resultado de la operación.</returns>
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

        /// <summary>
        /// Elimina una categoría de producto si no tiene dependencias.
        /// </summary>
        /// <param name="id">ID de la categoría a eliminar.</param>
        /// <returns>Redirección a la vista de índice.</returns>
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

        /// <summary>
        /// Renderiza una vista parcial como una cadena de texto HTML.
        /// </summary>
        /// <param name="viewName">Nombre de la vista parcial.</param>
        /// <param name="model">Modelo a enviar a la vista.</param>
        /// <returns>Cadena con el contenido HTML de la vista.</returns>
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
