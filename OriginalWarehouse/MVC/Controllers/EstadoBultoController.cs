using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador para la gestión de los estados de los bultos en el almacén.
    /// Permite listar, filtrar, crear, editar y eliminar estados de bultos.
    /// </summary>
    public class EstadoBultoController : Controller
    {
        private readonly IEstadoBultoManager _estadoBultoManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de estados de bultos.
        /// </summary>
        /// <param name="estadoBultoManager">Gestor de estados de bultos.</param>
        /// <param name="viewEngine">Motor de renderizado de vistas parciales.</param>
        public EstadoBultoController(IEstadoBultoManager estadoBultoManager, ICompositeViewEngine viewEngine)
        {
            _estadoBultoManager = estadoBultoManager;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// Muestra la lista de estados de bultos con paginación.
        /// </summary>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Cantidad de registros por página.</param>
        /// <returns>Vista con la lista de estados de bultos paginados.</returns>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var estados = await _estadoBultoManager.ObtenerTodos();

            int totalRegistros = estados.Count();

            // Paginación
            var estadosPaginados = estados
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(estadosPaginados);
        }

        /// <summary>
        /// Muestra la vista parcial para la creación o edición de un estado de bulto.
        /// </summary>
        /// <param name="id">ID del estado de bulto (opcional).</param>
        /// <returns>Vista parcial con el formulario de edición o creación.</returns>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            EstadoBulto estado = id.HasValue && id > 0
                ? await _estadoBultoManager.ObtenerPorId(id.Value)
                : new EstadoBulto();

            return PartialView("_EditCreatePartial", estado);
        }

        /// <summary>
        /// Guarda un nuevo estado de bulto o actualiza uno existente.
        /// </summary>
        /// <param name="estado">Objeto de estado de bulto a guardar.</param>
        /// <returns>Json con el resultado de la operación.</returns>
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

        /// <summary>
        /// Elimina un estado de bulto si no tiene dependencias.
        /// </summary>
        /// <param name="id">ID del estado de bulto a eliminar.</param>
        /// <returns>Redirección a la vista de índice.</returns>
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
