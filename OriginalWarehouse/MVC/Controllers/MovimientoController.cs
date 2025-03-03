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
    /// Controlador para gestionar los movimientos de bultos dentro del almacén.
    /// </summary>
    public class MovimientoController : Controller
    {
        private readonly IMovimientoManager _movimientoManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly IBultoManager _bultoManager;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Constructor del controlador de movimientos.
        /// </summary>
        /// <param name="movimientoManager">Gestor de movimientos.</param>
        /// <param name="userManager">Gestor de usuarios.</param>
        /// <param name="bultoManager">Gestor de bultos.</param>
        /// <param name="viewEngine">Motor de vistas.</param>
        public MovimientoController(IMovimientoManager movimientoManager, UserManager<Usuario> userManager, IBultoManager bultoManager, ICompositeViewEngine viewEngine)
        {
            _movimientoManager = movimientoManager;
            _userManager = userManager;
            _bultoManager = bultoManager;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// Muestra la lista de movimientos con filtros y paginación.
        /// </summary>
        /// <returns>Vista con la lista de movimientos.</returns>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string usuario = "", string bulto = "", string ubicacionOrigen = "", string ubicacionDestino = "")
        {
            var movimientos = await _movimientoManager.ObtenerTodos();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(usuario))
                movimientos = movimientos.Where(m => m.Usuario != null && m.Usuario.UserName.Equals(usuario, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(bulto))
                movimientos = movimientos.Where(m => m.Bulto != null && m.Bulto.Descripcion.Equals(bulto, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(ubicacionOrigen))
                movimientos = movimientos.Where(m => !string.IsNullOrEmpty(m.UbicacionOrigen) && m.UbicacionOrigen.Equals(ubicacionOrigen, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(ubicacionDestino))
                movimientos = movimientos.Where(m => !string.IsNullOrEmpty(m.UbicacionDestino) && m.UbicacionDestino.Equals(ubicacionDestino, StringComparison.OrdinalIgnoreCase)).ToList();

            int totalRegistros = movimientos.Count();

            // Paginación
            var movimientosPaginados = movimientos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.UsuarioFiltro = usuario;
            ViewBag.BultoFiltro = bulto;
            ViewBag.UbicacionOrigenFiltro = ubicacionOrigen;
            ViewBag.UbicacionDestinoFiltro = ubicacionDestino;

            // Pasamos las opciones únicas a la vista
            ViewBag.Usuarios = movimientos.Select(m => m.Usuario?.UserName).Distinct().Where(u => !string.IsNullOrEmpty(u)).ToList();
            ViewBag.Bultos = movimientos.Select(m => m.Bulto?.Descripcion).Distinct().Where(b => !string.IsNullOrEmpty(b)).ToList();
            ViewBag.UbicacionesOrigen = movimientos.Select(m => m.UbicacionOrigen).Distinct().Where(u => !string.IsNullOrEmpty(u)).ToList();
            ViewBag.UbicacionesDestino = movimientos.Select(m => m.UbicacionDestino).Distinct().Where(u => !string.IsNullOrEmpty(u)).ToList();


            return View(movimientosPaginados);
        }

        /// <summary>
        /// Carga la vista parcial para editar o crear un movimiento.
        /// </summary>
        /// <param name="id">Identificador del movimiento.</param>
        /// <returns>Vista parcial de edición/creación.</returns>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Movimiento movimiento = id.HasValue && id > 0
                ? await _movimientoManager.ObtenerPorId(id.Value)
                : new Movimiento { Fecha = DateTime.Now };

            return PartialView("_EditCreatePartial", movimiento);
        }

        /// <summary>
        /// Guarda un movimiento nuevo o actualiza uno existente.
        /// </summary>
        /// <param name="movimiento">Objeto de movimiento a guardar.</param>
        /// <returns>JSON con el estado de la operación.</returns>
        [HttpPost]
        public async Task<IActionResult> Save(Movimiento movimiento)
        {
            ModelState.Remove(nameof(movimiento.Usuario));
            ModelState.Remove(nameof(movimiento.Bulto));

            if (ModelState.IsValid)
            {
                if (movimiento.Id == 0)
                {
                    await _movimientoManager.Crear(movimiento);

                    var bulto = await _bultoManager.ObtenerPorId(movimiento.BultoId);
                    if (bulto != null)
                    {
                        bulto.UbicacionActual = movimiento.UbicacionDestino;
                        await _bultoManager.Actualizar(bulto);
                    }
                }
                else
                {
                    var movimientoExistente = await _movimientoManager.ObtenerPorId(movimiento.Id);
                    if (movimientoExistente == null) return NotFound();

                    movimientoExistente.Fecha = movimiento.Fecha;
                    movimientoExistente.UsuarioId = movimiento.UsuarioId;
                    movimientoExistente.BultoId = movimiento.BultoId;
                    movimientoExistente.UbicacionOrigen = movimiento.UbicacionOrigen;
                    movimientoExistente.UbicacionDestino = movimiento.UbicacionDestino;

                    await _movimientoManager.Actualizar(movimientoExistente);

                    var bulto = await _bultoManager.ObtenerPorId(movimiento.BultoId);
                    if (bulto != null && bulto.UbicacionActual != movimiento.UbicacionDestino)
                    {
                        bulto.UbicacionActual = movimiento.UbicacionDestino;
                        await _bultoManager.Actualizar(bulto);
                    }
                }

                return Json(new { success = true, message = "Movimiento guardado correctamente." });
            }

            await CargarListas();
            var html = await RenderPartialViewToString("_EditCreatePartial", movimiento);
            return Json(new { success = false, html });
        }

        /// <summary>
        /// Elimina un movimiento del sistema.
        /// </summary>
        /// <param name="id">Identificador del movimiento a eliminar.</param>
        /// <returns>Redirección a la vista de índice.</returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var movimiento = await _movimientoManager.ObtenerPorId(id);
            if (movimiento == null) return NotFound();

            await _movimientoManager.Eliminar(id);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Exporta la lista de movimientos a un archivo Excel.
        /// </summary>
        /// <returns>Archivo Excel con los movimientos.</returns>
        public async Task<IActionResult> ExportarExcel()
        {
            var movimientos = await _movimientoManager.ObtenerTodos();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Movimientos");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Fecha";
                worksheet.Cells[1, 3].Value = "Usuario";
                worksheet.Cells[1, 4].Value = "Bulto";
                worksheet.Cells[1, 5].Value = "Ubicación Origen";
                worksheet.Cells[1, 6].Value = "Ubicación Destino";

                using (var headerRange = worksheet.Cells["A1:F1"])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }

                int row = 2;
                foreach (var movimiento in movimientos)
                {
                    worksheet.Cells[row, 1].Value = movimiento.Id;
                    worksheet.Cells[row, 2].Value = movimiento.Fecha.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[row, 3].Value = movimiento.Usuario?.UserName ?? "N/A"; // Corregido
                    worksheet.Cells[row, 4].Value = movimiento.Bulto?.Descripcion ?? "N/A";
                    worksheet.Cells[row, 5].Value = movimiento.UbicacionOrigen;
                    worksheet.Cells[row, 6].Value = movimiento.UbicacionDestino;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var fileContent = package.GetAsByteArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Movimientos.xlsx");
            }
        }

        /// <summary>
        /// Renderiza una vista parcial como cadena de texto.
        /// </summary>
        /// <param name="viewName">Nombre de la vista parcial.</param>
        /// <param name="model">Modelo de datos para la vista.</param>
        /// <returns>Vista parcial renderizada como cadena.</returns>
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
            ViewBag.Bultos = await _bultoManager.ObtenerTodos();
        }
    }
}
