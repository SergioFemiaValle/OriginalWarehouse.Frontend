using Microsoft.AspNetCore.Identity;
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
    /// Controlador para la gestión de las salidas de productos del almacén.
    /// </summary>
    public class SalidaController : Controller
    {
        private readonly ISalidaManager _salidaManager;
        private readonly IBultoManager _bultoManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly IDetalleBultoManager _detalleBultoManager;
        private readonly IProductoManager _productoManager;

        /// <summary>
        /// Constructor de <see cref="SalidaController"/>.
        /// </summary>
        /// <param name="salidaManager">Gestor de salidas.</param>
        /// <param name="bultoManager">Gestor de bultos.</param>
        /// <param name="userManager">Gestor de usuarios.</param>
        /// <param name="viewEngine">Motor de vistas.</param>
        public SalidaController(ISalidaManager salidaManager, IBultoManager bultoManager,
            UserManager<Usuario> userManager, ICompositeViewEngine viewEngine,
            IDetalleBultoManager detalleBultoManager, IProductoManager productoManager)
        {
            _salidaManager = salidaManager;
            _bultoManager = bultoManager;
            _userManager = userManager;
            _viewEngine = viewEngine;
            _detalleBultoManager = detalleBultoManager;
            _productoManager = productoManager;
        }

        /// <summary>
        /// Muestra la lista de salidas con filtros y paginación.
        /// </summary>
        /// <param name="page">Número de la página.</param>
        /// <param name="pageSize">Cantidad de registros por página.</param>
        /// <param name="usuario">Filtro por usuario.</param>
        /// <param name="bulto">Filtro por bulto.</param>
        /// <returns>Vista con la lista de salidas paginadas.</returns>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string usuario = "", string bulto = "")
        {
            var salidas = await _salidaManager.ObtenerTodas();

            if (!string.IsNullOrEmpty(usuario))
            {
                salidas = salidas.Where(s => s.Usuario != null && s.Usuario.UserName.Contains(usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(bulto))
            {
                salidas = salidas.Where(s => s.Bulto != null && s.Bulto.Descripcion.Contains(bulto, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRegistros = salidas.Count();
            var salidasPaginadas = salidas.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.UsuarioFiltro = usuario;
            ViewBag.BultoFiltro = bulto;

            return View(salidasPaginadas);
        }

        /// <summary>
        /// Carga la vista parcial para editar o crear una salida.
        /// </summary>
        /// <param name="id">Identificador de la salida.</param>
        /// <returns>Vista parcial con el formulario de edición o creación.</returns>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Salida salida = id.HasValue && id > 0
                ? await _salidaManager.ObtenerPorId(id.Value)
                : new Salida { Fecha = DateTime.Now };

            return PartialView("_EditCreatePartial", salida);
        }

        /// <summary>
        /// Guarda una salida nueva o actualiza una existente.
        /// </summary>
        /// <param name="salida">Objeto de la salida a guardar.</param>
        /// <returns>JSON con el estado de la operación.</returns>
        [HttpPost]
        public async Task<IActionResult> Save(Salida salida)
        {
            ModelState.Remove(nameof(salida.Usuario));
            ModelState.Remove(nameof(salida.Bulto));

            if (ModelState.IsValid)
            {
                // Verificar si el bulto ya tiene una salida asociada
                var salidaExistentePorBulto = (await _salidaManager.ObtenerTodas()).Where(e => e.BultoId == salida.BultoId);
                if (salida.Id == 0 && salidaExistentePorBulto.Any())
                {
                    return Json(new { success = true, message = "Este bulto ya tiene una salida registrada." });
                }

                if (salida.Id == 0) // Nueva salida
                {
                    var bulto = await _bultoManager.ObtenerPorId(salida.BultoId);
                    if (bulto == null) return NotFound();

                    var detalles = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == salida.BultoId).ToList();

                    if (!detalles.Any())
                    {
                        return Json(new { success = true, message = "No se puede realizar una salida sin detalles." });
                    }

                    // Simular stock final y verificar que no quede negativo
                    var productosAfectados = new Dictionary<int, int>();

                    foreach (var detalle in detalles)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            productosAfectados[detalle.ProductoId] = producto.CantidadEnStock - detalle.Cantidad;
                        }
                    }

                    // Comprobar stock suficiente
                    var productosConStockNegativo = productosAfectados.Where(p => p.Value < 0).ToList();
                    if (productosConStockNegativo.Any())
                    {
                        var mensaje = string.Join(", ", productosConStockNegativo.Select(p => $"Producto ID {p.Key}: stock insuficiente"));
                        return Json(new { success = true, message = $"No se puede realizar la salida. {mensaje}" });
                    }

                    // Si todo OK, aplicar cambios reales
                    foreach (var detalle in detalles)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.CantidadEnStock -= detalle.Cantidad;
                            await _productoManager.Actualizar(producto);
                        }
                    }

                    await _salidaManager.Crear(salida);
                }
                else // Actualización de una salida existente
                {
                    var salidaExistente = await _salidaManager.ObtenerPorId(salida.Id);
                    if (salidaExistente == null) return NotFound();

                    var detallesAntiguos = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == salidaExistente.BultoId).ToList();
                    var detallesNuevos = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == salida.BultoId).ToList();

                    // Simular stock final
                    var productosAfectados = new Dictionary<int, int>();

                    var productosIds = detallesAntiguos.Select(d => d.ProductoId)
                                                       .Union(detallesNuevos.Select(d => d.ProductoId))
                                                       .Distinct();

                    foreach (var productoId in productosIds)
                    {
                        var producto = await _productoManager.ObtenerPorId(productoId);
                        if (producto != null)
                        {
                            productosAfectados[productoId] = producto.CantidadEnStock;
                        }
                    }

                    // Revertir salida anterior (sumar stock)
                    foreach (var detalle in detallesAntiguos)
                    {
                        if (productosAfectados.ContainsKey(detalle.ProductoId))
                        {
                            productosAfectados[detalle.ProductoId] += detalle.Cantidad;
                        }
                    }

                    // Simular nueva salida (restar stock)
                    foreach (var detalle in detallesNuevos)
                    {
                        if (productosAfectados.ContainsKey(detalle.ProductoId))
                        {
                            productosAfectados[detalle.ProductoId] -= detalle.Cantidad;
                        }
                    }

                    // Verificar que no queden productos con stock negativo
                    var productosConStockNegativo = productosAfectados.Where(p => p.Value < 0).ToList();
                    if (productosConStockNegativo.Any())
                    {
                        var mensaje = string.Join(", ", productosConStockNegativo.Select(p => $"Producto ID {p.Key}: stock insuficiente"));
                        return Json(new { success = false, message = $"No se puede realizar la actualización. {mensaje}" });
                    }

                    // Revertir detalles antiguos (sumar stock)
                    foreach (var detalle in detallesAntiguos)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.CantidadEnStock += detalle.Cantidad;
                            await _productoManager.Actualizar(producto);
                        }
                    }

                    // Aplicar nuevos detalles (restar stock)
                    foreach (var detalle in detallesNuevos)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.CantidadEnStock -= detalle.Cantidad;
                            await _productoManager.Actualizar(producto);
                        }
                    }

                    // Actualizar salida
                    salidaExistente.Fecha = salida.Fecha;
                    salidaExistente.UsuarioId = salida.UsuarioId;
                    salidaExistente.BultoId = salida.BultoId;

                    await _salidaManager.Actualizar(salidaExistente);
                }

                return Json(new { success = true, message = "Salida guardada correctamente." });
            }

            await CargarListas();
            var html = await RenderPartialViewToString("_EditCreatePartial", salida);
            return Json(new { success = false, html });
        }


        /// <summary>
        /// Elimina una salida por su identificador.
        /// </summary>
        /// <param name="id">ID de la salida a eliminar.</param>
        /// <returns>Redirección a la vista de índice.</returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var salida = await _salidaManager.ObtenerPorId(id);
                if (salida == null) return NotFound();

                // Obtener los productos en DetalleBulto y revertir stock antes de eliminar la salida
                var detalles = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == salida.BultoId);
                foreach (var detalle in detalles)
                {
                    var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                    if (producto != null)
                    {
                        producto.CantidadEnStock += detalle.Cantidad;
                        await _productoManager.Actualizar(producto);
                    }
                }

                await _salidaManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar la salida. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }


        /// <summary>
        /// Exporta la lista de salidas a un archivo Excel.
        /// </summary>
        /// <returns>Archivo Excel con los datos de las salidas.</returns>
        public async Task<IActionResult> ExportarExcel()
        {
            var salidas = await _salidaManager.ObtenerTodas();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Salidas");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Fecha";
                worksheet.Cells[1, 3].Value = "Usuario";
                worksheet.Cells[1, 4].Value = "Bulto";

                using (var headerRange = worksheet.Cells["A1:D1"])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                }

                int row = 2;
                foreach (var salida in salidas)
                {
                    worksheet.Cells[row, 1].Value = salida.Id;
                    worksheet.Cells[row, 2].Value = salida.Fecha.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[row, 3].Value = salida.Usuario?.UserName ?? "N/A";
                    worksheet.Cells[row, 4].Value = salida.Bulto?.Descripcion ?? "N/A";
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var fileContent = package.GetAsByteArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Salidas.xlsx");
            }
        }

        private async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var writer = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);
                if (!viewResult.Success) throw new InvalidOperationException($"No se encontró la vista: {viewName}");

                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, writer, new HtmlHelperOptions());
                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }

        private async Task CargarListas()
        {
            ViewBag.Usuarios = _userManager.Users.ToList();

            var bultos = await _bultoManager.ObtenerTodos();
            var detalles = await _detalleBultoManager.ObtenerTodos();
            var salidas = await _salidaManager.ObtenerTodas();

            var bultosConDetallesIds = detalles.Select(d => d.BultoId).Distinct();
            var bultosConSalidaIds = salidas.Select(s => s.BultoId).Distinct();

            ViewBag.Bultos = bultos
                .Where(b => bultosConDetallesIds.Contains(b.Id) && !bultosConSalidaIds.Contains(b.Id))
                .ToList();
        }
    }
}
