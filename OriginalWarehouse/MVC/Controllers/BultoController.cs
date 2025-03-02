using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OfficeOpenXml;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador para la gestión de bultos en el almacén.
    /// Permite listar, filtrar, crear, editar, eliminar y exportar bultos en formato Excel.
    /// </summary>
    public class BultoController : Controller
    {
        private readonly IBultoManager _bultoManager;
        private readonly IEstadoBultoManager _estadoBultoManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de bultos.
        /// </summary>
        /// <param name="bultoManager">Gestor de bultos.</param>
        /// <param name="estadoBultoManager">Gestor de estados de bulto.</param>
        /// <param name="viewEngine">Motor de renderizado de vistas parciales.</param>
        public BultoController(IBultoManager bultoManager, IEstadoBultoManager estadoBultoManager, ICompositeViewEngine viewEngine)
        {
            _bultoManager = bultoManager;
            _estadoBultoManager = estadoBultoManager;
            _viewEngine = viewEngine;
        }

        #region public methods

        /// <summary>
        /// Muestra la lista de bultos con filtros y paginación.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string ubicacion = "", string estado = "")
        {
            var bultos = await _bultoManager.ObtenerTodos();

            // Filtrado por Ubicación
            if (!string.IsNullOrEmpty(ubicacion))
            {
                bultos = bultos.Where(b => !string.IsNullOrEmpty(b.UbicacionActual) && b.UbicacionActual.Equals(ubicacion, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filtrado por Estado
            if (!string.IsNullOrEmpty(estado))
            {
                bultos = bultos.Where(b => b.Estado != null && b.Estado.Nombre.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRegistros = bultos.Count();
            var bultosPaginados = bultos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.UbicacionFiltro = ubicacion;
            ViewBag.EstadoFiltro = estado;

            ViewBag.Ubicaciones = bultos.Select(b => b.UbicacionActual).Distinct().Where(u => !string.IsNullOrEmpty(u)).ToList();
            ViewBag.Estados = bultos.Select(b => b.Estado?.Nombre).Distinct().Where(e => !string.IsNullOrEmpty(e)).ToList();

            return View(bultosPaginados);
        }

        /// <summary>
        /// Muestra la vista parcial para la creación o edición de un bulto.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Bulto bulto = id.HasValue && id > 0
                ? await _bultoManager.ObtenerPorId(id.Value)
                : new Bulto();

            return PartialView("_EditCreatePartial", bulto);
        }

        /// <summary>
        /// Guarda un nuevo bulto o actualiza uno existente.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Bulto bulto)
        {
            ModelState.Remove(nameof(bulto.Detalles));
            ModelState.Remove(nameof(bulto.Movimientos));
            ModelState.Remove(nameof(bulto.Entradas));
            ModelState.Remove(nameof(bulto.Salidas));
            ModelState.Remove(nameof(bulto.Estado));

            if (ModelState.IsValid)
            {
                if (bulto.Id == 0)
                {
                    await _bultoManager.Crear(bulto);
                }
                else
                {
                    var bultoExistente = await _bultoManager.ObtenerPorId(bulto.Id);
                    if (bultoExistente == null) return NotFound();

                    bultoExistente.Descripcion = bulto.Descripcion;
                    bultoExistente.UbicacionActual = bulto.UbicacionActual;
                    bultoExistente.EstadoId = bulto.EstadoId;

                    await _bultoManager.Actualizar(bultoExistente);
                }

                return Json(new { success = true, message = "Bulto guardado correctamente." });
            }

            await CargarListas();
            var html = await RenderPartialViewToString("_EditCreatePartial", bulto);
            return Json(new { success = false, html });
        }

        /// <summary>
        /// Elimina un bulto si no tiene dependencias.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var bulto = await _bultoManager.ObtenerPorId(id);
                if (bulto == null) return NotFound();

                await _bultoManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el bulto. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Exporta la lista de bultos a un archivo de Excel.
        /// </summary>
        public async Task<IActionResult> ExportarExcel()
        {
            var bultos = await _bultoManager.ObtenerTodos();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Bultos");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Descripción";
                worksheet.Cells[1, 3].Value = "Ubicación Actual";
                worksheet.Cells[1, 4].Value = "Estado";

                int row = 2;
                foreach (var bulto in bultos)
                {
                    worksheet.Cells[row, 1].Value = bulto.Id;
                    worksheet.Cells[row, 2].Value = bulto.Descripcion;
                    worksheet.Cells[row, 3].Value = bulto.UbicacionActual;
                    worksheet.Cells[row, 4].Value = bulto.Estado?.Nombre ?? "N/A";
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var fileContent = package.GetAsByteArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Bultos.xlsx");
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Renderiza una vista parcial como una cadena de texto HTML.
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

        /// <summary>
        /// Carga las listas de estados de bultos para la vista.
        /// </summary>
        private async Task CargarListas()
        {
            ViewBag.Estados = await _estadoBultoManager.ObtenerTodos();
        }

        #endregion
    }
}
