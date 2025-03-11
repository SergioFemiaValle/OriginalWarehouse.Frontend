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
    /// Controlador para la gestión de entradas de productos en el almacén.
    /// Permite listar, filtrar, crear, editar, eliminar y exportar entradas en formato Excel.
    /// </summary>
    public class EntradaController : Controller
    {
        private readonly IEntradaManager _entradaManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly IBultoManager _bultoManager;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly IDetalleBultoManager _detalleBultoManager;
        private readonly IProductoManager _productoManager;

        /// <summary>
        /// Constructor del controlador de entradas.
        /// </summary>
        public EntradaController(IEntradaManager entradaManager, UserManager<Usuario> userManager,
            IBultoManager bultoManager, ICompositeViewEngine viewEngine,
            IDetalleBultoManager detalleBultoManager, IProductoManager productoManager)
        {
            _entradaManager = entradaManager;
            _userManager = userManager;
            _bultoManager = bultoManager;
            _viewEngine = viewEngine;
            _detalleBultoManager = detalleBultoManager;
            _productoManager = productoManager;
        }

        /// <summary>
        /// Muestra la lista de entradas con filtros y paginación.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string usuario = "", string bulto = "")
        {
            var entradas = await _entradaManager.ObtenerTodas();

            if (!string.IsNullOrEmpty(usuario))
            {
                entradas = entradas.Where(e => e.Usuario != null && e.Usuario.UserName.Contains(usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(bulto))
            {
                entradas = entradas.Where(e => e.Bulto != null && e.Bulto.Descripcion.Contains(bulto, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRegistros = entradas.Count();
            var entradasPaginadas = entradas.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.UsuarioFiltro = usuario;
            ViewBag.BultoFiltro = bulto;

            return View(entradasPaginadas);
        }

        /// <summary>
        /// Muestra la vista parcial para la creación o edición de una entrada.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Entrada entrada = id.HasValue && id > 0
                ? await _entradaManager.ObtenerPorId(id.Value)
                : new Entrada { Fecha = DateTime.Now };

            return PartialView("_EditCreatePartial", entrada);
        }

        /// <summary>
        /// Guarda una nueva entrada o actualiza una existente.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Entrada entrada)
        {
            ModelState.Remove(nameof(entrada.Usuario));
            ModelState.Remove(nameof(entrada.Bulto));

            if (ModelState.IsValid)
            {
                // Verificar si el bulto ya tiene una entrada asociada
                var entradaExistentePorBulto = (await _entradaManager.ObtenerTodas()).Where(e => e.BultoId == entrada.BultoId);
                if (entrada.Id == 0 && entradaExistentePorBulto.Any())
                {
                    return Json(new { success = true, message = "Este bulto ya tiene una entrada registrada." });
                }

                if (entrada.Id == 0) // Nueva entrada
                {
                    // Obtener los productos en DetalleBulto
                    var detalles = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == entrada.BultoId);

                    if (!detalles.Any())
                    {
                        return Json(new { success = true, message = "No se puede crear una entrada sin detalles." });
                    }

                    foreach (var detalle in detalles)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.CantidadEnStock += detalle.Cantidad;
                            await _productoManager.Actualizar(producto);
                        }
                    }

                    await _entradaManager.Crear(entrada);
                }
                else // Actualización de una entrada existente
                {
                    var entradaExistente = await _entradaManager.ObtenerPorId(entrada.Id);
                    if (entradaExistente == null) return NotFound();

                    // Obtener detalles antiguos y nuevos
                    var detallesAnteriores = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == entradaExistente.BultoId).ToList();
                    var detallesNuevos = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == entrada.BultoId).ToList();

                    // Preparar diccionario para simular stock
                    var productosAfectados = new Dictionary<int, int>();

                    var productosIds = detallesAnteriores.Select(d => d.ProductoId)
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

                    // Simular reversión del stock anterior
                    foreach (var detalle in detallesAnteriores)
                    {
                        if (productosAfectados.ContainsKey(detalle.ProductoId))
                        {
                            productosAfectados[detalle.ProductoId] -= detalle.Cantidad;
                        }
                    }

                    // Simular aplicación de nuevos detalles
                    foreach (var detalle in detallesNuevos)
                    {
                        if (productosAfectados.ContainsKey(detalle.ProductoId))
                        {
                            productosAfectados[detalle.ProductoId] += detalle.Cantidad;
                        }
                    }

                    // Comprobar que ningún stock queda negativo
                    var productosConStockNegativo = productosAfectados.Where(p => p.Value < 0).ToList();

                    if (productosConStockNegativo.Any())
                    {
                        var mensaje = string.Join(", ", productosConStockNegativo.Select(p => $"Producto ID {p.Key}: stock insuficiente"));
                        return Json(new { success = true, message = $"No se puede realizar la operación. {mensaje}" });
                    }

                    // Revertir el stock de los productos de la entrada anterior
                    //var detallesAnteriores = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == entradaExistente.BultoId);
                    foreach (var detalle in detallesAnteriores)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.CantidadEnStock -= detalle.Cantidad;
                            await _productoManager.Actualizar(producto);
                        }
                    }

                    // Actualizar la entrada
                    entradaExistente.Fecha = entrada.Fecha;
                    entradaExistente.UsuarioId = entrada.UsuarioId;
                    entradaExistente.BultoId = entrada.BultoId;

                    await _entradaManager.Actualizar(entradaExistente);

                    // Aplicar los nuevos detalles de la entrada
                    //var detallesNuevos = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == entrada.BultoId);
                    foreach (var detalle in detallesNuevos)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.CantidadEnStock += detalle.Cantidad;
                            await _productoManager.Actualizar(producto);
                        }
                    }
                }

                return Json(new { success = true, message = "Entrada guardada correctamente." });
            }

            await CargarListas();
            var html = await RenderPartialViewToString("_EditCreatePartial", entrada);
            return Json(new { success = false, html });
        }


        /// <summary>
        /// Elimina una entrada si no tiene dependencias.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entrada = await _entradaManager.ObtenerPorId(id);
                if (entrada == null) return NotFound();

                // Obtener los productos en DetalleBulto y revertir stock antes de eliminar la entrada
                var detalles = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == entrada.BultoId);
                foreach (var detalle in detalles)
                {
                    var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                    if (producto != null)
                    {
                        producto.CantidadEnStock -= detalle.Cantidad;
                        await _productoManager.Actualizar(producto);
                    }
                }

                await _entradaManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar la entrada. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }


        /// <summary>
        /// Exporta la lista de entradas a un archivo de Excel.
        /// </summary>
        public async Task<IActionResult> ExportarExcel()
        {
            var entradas = await _entradaManager.ObtenerTodas();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Entradas");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Fecha";
                worksheet.Cells[1, 3].Value = "Usuario";
                worksheet.Cells[1, 4].Value = "Bulto";

                using (var headerRange = worksheet.Cells["A1:D1"])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                int row = 2;
                foreach (var entrada in entradas)
                {
                    worksheet.Cells[row, 1].Value = entrada.Id;
                    worksheet.Cells[row, 2].Value = entrada.Fecha.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[row, 3].Value = entrada.Usuario?.UserName ?? "N/A"; // Corregido
                    worksheet.Cells[row, 4].Value = entrada.Bulto?.Descripcion ?? "N/A";
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var fileContent = package.GetAsByteArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Entradas.xlsx");
            }
        }

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

                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, writer, new HtmlHelperOptions());
                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }

        /// <summary>
        /// Carga las listas de usuarios y bultos para la vista.
        /// </summary>
        private async Task CargarListas()
        {
            ViewBag.Usuarios = _userManager.Users.ToList();

            var bultos = await _bultoManager.ObtenerTodos();
            var detalles = await _detalleBultoManager.ObtenerTodos();
            var entradas = await _entradaManager.ObtenerTodas();

            var bultosConDetallesIds = detalles.Select(d => d.BultoId).Distinct();
            var bultosConEntradaIds = entradas.Select(e => e.BultoId).Distinct();

            ViewBag.Bultos = bultos
                .Where(b => bultosConDetallesIds.Contains(b.Id) && !bultosConEntradaIds.Contains(b.Id))
                .ToList();
        }

    }
}
