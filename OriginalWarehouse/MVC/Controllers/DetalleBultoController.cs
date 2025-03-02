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
    /// Controlador para la gestión de detalles de bultos en el almacén.
    /// Permite listar, crear, editar, eliminar y exportar detalles de bultos en formato Excel.
    /// </summary>
    public class DetalleBultoController : Controller
    {
        private readonly IDetalleBultoManager _detalleBultoManager;
        private readonly IProductoManager _productoManager;
        private readonly IBultoManager _bultoManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de detalles de bultos.
        /// </summary>
        public DetalleBultoController(IDetalleBultoManager detalleBultoManager, IProductoManager productoManager,
                                      IBultoManager bultoManager, ICompositeViewEngine viewEngine)
        {
            _detalleBultoManager = detalleBultoManager;
            _productoManager = productoManager;
            _bultoManager = bultoManager;
            _viewEngine = viewEngine;
        }

        #region public methods

        /// <summary>
        /// Muestra la lista de detalles de bultos con filtros y paginación.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string nombre = "", string lote = "")
        {
            var detalles = await _detalleBultoManager.ObtenerTodos();

            if (!string.IsNullOrEmpty(nombre))
            {
                detalles = detalles.Where(d => d.Producto != null && d.Producto.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(lote))
            {
                detalles = detalles.Where(d => !string.IsNullOrEmpty(d.Lote) && d.Lote.Contains(lote, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRegistros = detalles.Count();
            var detallesPaginados = detalles.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.NombreFiltro = nombre;
            ViewBag.LoteFiltro = lote;

            return View(detallesPaginados);
        }

        /// <summary>
        /// Muestra la vista parcial para la creación o edición de un detalle de bulto.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            DetalleBulto detalle = id.HasValue && id > 0
                ? await _detalleBultoManager.ObtenerPorId(id.Value)
                : new DetalleBulto();

            return PartialView("_EditCreatePartial", detalle);
        }

        /// <summary>
        /// Guarda un nuevo detalle de bulto o actualiza uno existente.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(DetalleBulto detalle)
        {
            ModelState.Remove(nameof(detalle.Bulto));
            ModelState.Remove(nameof(detalle.Producto));

            if (ModelState.IsValid)
            {
                if (detalle.Id == 0)
                {
                    await _detalleBultoManager.Crear(detalle);
                }
                else
                {
                    var detalleExistente = await _detalleBultoManager.ObtenerPorId(detalle.Id);
                    if (detalleExistente == null) return NotFound();

                    detalleExistente.BultoId = detalle.BultoId;
                    detalleExistente.ProductoId = detalle.ProductoId;
                    detalleExistente.Cantidad = detalle.Cantidad;
                    detalleExistente.Lote = detalle.Lote;
                    detalleExistente.FechaDeCaducidad = detalle.FechaDeCaducidad;

                    await _detalleBultoManager.Actualizar(detalleExistente);
                }

                return Json(new { success = true, message = "Detalle de Bulto guardado correctamente." });
            }

            await CargarListas();
            var html = await RenderPartialViewToString("_EditCreatePartial", detalle);
            return Json(new { success = false, html });
        }

        /// <summary>
        /// Elimina un detalle de bulto si no tiene dependencias.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var detalle = await _detalleBultoManager.ObtenerPorId(id);
                if (detalle == null) return NotFound();

                await _detalleBultoManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el detalle de bulto. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Exporta la lista de detalles de bultos a un archivo de Excel.
        /// </summary>
        public async Task<IActionResult> ExportarExcel()
        {
            var detallesBulto = await _detalleBultoManager.ObtenerTodos();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Detalles de Bulto");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Bulto";
                worksheet.Cells[1, 3].Value = "Producto";
                worksheet.Cells[1, 4].Value = "Cantidad";
                worksheet.Cells[1, 5].Value = "Lote";
                worksheet.Cells[1, 6].Value = "Fecha de Caducidad";

                int row = 2;
                foreach (var detalle in detallesBulto)
                {
                    worksheet.Cells[row, 1].Value = detalle.Id;
                    worksheet.Cells[row, 2].Value = detalle.Bulto?.Descripcion ?? "N/A";
                    worksheet.Cells[row, 3].Value = detalle.Producto?.Nombre ?? "N/A";
                    worksheet.Cells[row, 4].Value = detalle.Cantidad;
                    worksheet.Cells[row, 5].Value = detalle.Lote ?? "Sin Lote";
                    worksheet.Cells[row, 6].Value = detalle.FechaDeCaducidad?.ToString("dd/MM/yyyy") ?? "Sin fecha";
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var fileContent = package.GetAsByteArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DetallesBulto.xlsx");
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
        /// Carga las listas de productos y bultos para la vista.
        /// </summary>
        private async Task CargarListas()
        {
            ViewBag.Productos = await _productoManager.ObtenerTodos();
            ViewBag.Bultos = await _bultoManager.ObtenerTodos();
        }

        #endregion
    }
}
