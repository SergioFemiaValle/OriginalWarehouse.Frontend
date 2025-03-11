using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OfficeOpenXml;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Application.Managers;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Models;

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
        private readonly IDetalleBultoManager _detalleBultoManager;
        private readonly IProductoManager _productoManager;
        private readonly IEntradaManager _entradaManager;
        private readonly ISalidaManager _salidaManager;

        /// <summary>
        /// Constructor del controlador de bultos.
        /// </summary>
        /// <param name="bultoManager">Gestor de bultos.</param>
        /// <param name="estadoBultoManager">Gestor de estados de bulto.</param>
        /// <param name="viewEngine">Motor de renderizado de vistas parciales.</param>
        public BultoController(IBultoManager bultoManager, IEstadoBultoManager estadoBultoManager,
            ICompositeViewEngine viewEngine, IDetalleBultoManager detalleBultoManager, IProductoManager productoManager, IEntradaManager entradaManager, ISalidaManager salidaManager)
        {
            _bultoManager = bultoManager;
            _estadoBultoManager = estadoBultoManager;
            _viewEngine = viewEngine;
            _detalleBultoManager = detalleBultoManager;
            _productoManager = productoManager;
            _entradaManager = entradaManager;
            _salidaManager = salidaManager;
        }

        #region public methods

        /// <summary>
        /// Muestra la lista de bultos con filtros y paginación.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string ubicacion = "", string estado = "")
        {
            var bultos = await _bultoManager.ObtenerTodos();
            var entradas = await _entradaManager.ObtenerTodas();
            var salidas = await _salidaManager.ObtenerTodas();

            // IDs de bultos con entradas/salidas
            var bultosConEntrada = entradas.Select(e => e.BultoId).Distinct().ToList();
            var bultosConSalida = salidas.Select(s => s.BultoId).Distinct().ToList();

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

            // Enriquecer los bultos con información de entrada/salida
            var bultosConEstado = bultos.Select(b => new BultoConEstado
            {
                Bulto = b,
                TieneEntrada = bultosConEntrada.Contains(b.Id),
                TieneSalida = bultosConSalida.Contains(b.Id)
            }).ToList();

            // Paginación
            int totalRegistros = bultosConEstado.Count();
            var bultosPaginados = bultosConEstado.Skip((page - 1) * pageSize).Take(pageSize).ToList();

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

                // Verificar si el bulto tiene registros en Entrada o Salida
                var tieneEntrada = (await _entradaManager.ObtenerTodas()).Where(e => e.BultoId == id);
                var tieneSalida = (await _salidaManager.ObtenerTodas()).Where(s => s.BultoId == id);

                // Obtener los detalles del bulto antes de eliminarlo
                var detallesBulto = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == id);

                if (tieneEntrada.Any() || tieneSalida.Any())
                {
                    foreach (var detalle in detallesBulto)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            if (tieneEntrada.Any())
                            {
                                producto.CantidadEnStock -= detalle.Cantidad; // Reducir stock si estaba en una entrada
                                foreach(var entrada in tieneEntrada)
                                {
                                    await _entradaManager.Eliminar(entrada.Id);
                                }
                            }
                            if (tieneSalida.Any())
                            {
                                producto.CantidadEnStock += detalle.Cantidad; // Devolver stock si estaba en una salida
                                foreach(var salida in tieneSalida)
                                {
                                    await _salidaManager.Eliminar(salida.Id);
                                }
                            }
                            await _productoManager.Actualizar(producto);
                        }
                    }
                }

                // Eliminar los detalles antes de eliminar el bulto
                foreach (var detalle in detallesBulto)
                {
                    await _detalleBultoManager.Eliminar(detalle.Id);
                }

                // Eliminar el bulto
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

                // Encabezados personalizados
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Descripción";
                worksheet.Cells[1, 3].Value = "Ubicación Actual";
                worksheet.Cells[1, 4].Value = "Estado";

                // Aplicar estilos a los encabezados
                using (var headerRange = worksheet.Cells["A1:D1"])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

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
