using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador que gestiona la administración de tipos de almacenamiento especial.
    /// Permite listar, crear, editar y eliminar registros.
    /// </summary>
    public class AlmacenamientoEspecialController : Controller
    {
        private readonly IAlmacenamientoEspecialManager _almacenamientoEspecialManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de almacenamiento especial.
        /// </summary>
        /// <param name="almacenamientoEspecialManager">Gestor de almacenamiento especial.</param>
        /// <param name="viewEngine">Motor de renderizado de vistas parciales.</param>
        public AlmacenamientoEspecialController(IAlmacenamientoEspecialManager almacenamientoEspecialManager, ICompositeViewEngine viewEngine)
        {
            _almacenamientoEspecialManager = almacenamientoEspecialManager;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// Muestra la lista de tipos de almacenamiento especial con paginación.
        /// </summary>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Cantidad de registros por página.</param>
        /// <returns>Vista con los tipos de almacenamiento especial paginados.</returns>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var almacenamientos = await _almacenamientoEspecialManager.ObtenerTodos();
            int totalRegistros = almacenamientos.Count();

            // Paginación
            var almacenamientosPaginados = almacenamientos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(almacenamientosPaginados);
        }

        /// <summary>
        /// Muestra la vista parcial para la creación o edición de un tipo de almacenamiento especial.
        /// </summary>
        /// <param name="id">ID del almacenamiento especial (opcional).</param>
        /// <returns>Vista parcial con el formulario de edición o creación.</returns>
        [HttpGet]
        public IActionResult EditPartial(int? id)
        {
            AlmacenamientoEspecial almacenamiento = id.HasValue && id > 0
                ? _almacenamientoEspecialManager.ObtenerPorId(id.Value).Result
                : new AlmacenamientoEspecial();

            return PartialView("_EditCreatePartial", almacenamiento);
        }

        /// <summary>
        /// Guarda un nuevo tipo de almacenamiento especial o actualiza uno existente.
        /// </summary>
        /// <param name="almacenamiento">Objeto de almacenamiento especial a guardar.</param>
        /// <returns>Json con el resultado de la operación.</returns>
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

        /// <summary>
        /// Elimina un tipo de almacenamiento especial si no tiene dependencias.
        /// </summary>
        /// <param name="id">ID del almacenamiento especial a eliminar.</param>
        /// <returns>Redirección a la vista de índice.</returns>
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
    }
}
