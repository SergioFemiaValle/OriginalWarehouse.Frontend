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
    public class MovimientoController : Controller
    {
        private readonly IMovimientoManager _movimientoManager;
        private readonly UserManager<Usuario> _userManager; // Usamos Identity
        private readonly IBultoManager _bultoManager;
        private readonly ICompositeViewEngine _viewEngine;

        public MovimientoController(IMovimientoManager movimientoManager, UserManager<Usuario> userManager, IBultoManager bultoManager, ICompositeViewEngine viewEngine)
        {
            _movimientoManager = movimientoManager;
            _userManager = userManager; // Corregido
            _bultoManager = bultoManager;
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string usuario = "", string bulto = "", string ubicacionOrigen = "", string ubicacionDestino = "")
        {
            var movimientos = await _movimientoManager.ObtenerTodos();

            // 🔹 Filtrado por Usuario
            if (!string.IsNullOrEmpty(usuario))
            {
                movimientos = movimientos.Where(m => m.Usuario != null && m.Usuario.UserName.Equals(usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 🔹 Filtrado por Bulto
            if (!string.IsNullOrEmpty(bulto))
            {
                movimientos = movimientos.Where(m => m.Bulto != null && m.Bulto.Descripcion.Equals(bulto, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 🔹 Filtrado por Ubicación Origen
            if (!string.IsNullOrEmpty(ubicacionOrigen))
            {
                movimientos = movimientos.Where(m => !string.IsNullOrEmpty(m.UbicacionOrigen) && m.UbicacionOrigen.Equals(ubicacionOrigen, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 🔹 Filtrado por Ubicación Destino
            if (!string.IsNullOrEmpty(ubicacionDestino))
            {
                movimientos = movimientos.Where(m => !string.IsNullOrEmpty(m.UbicacionDestino) && m.UbicacionDestino.Equals(ubicacionDestino, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRegistros = movimientos.Count();

            // 🔹 Paginación
            var movimientosPaginados = movimientos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.UsuarioFiltro = usuario;
            ViewBag.BultoFiltro = bulto;
            ViewBag.UbicacionOrigenFiltro = ubicacionOrigen;
            ViewBag.UbicacionDestinoFiltro = ubicacionDestino;

            // 📌 Pasamos las opciones únicas a la vista
            ViewBag.Usuarios = movimientos.Select(m => m.Usuario?.UserName).Distinct().Where(u => !string.IsNullOrEmpty(u)).ToList();
            ViewBag.Bultos = movimientos.Select(m => m.Bulto?.Descripcion).Distinct().Where(b => !string.IsNullOrEmpty(b)).ToList();
            ViewBag.UbicacionesOrigen = movimientos.Select(m => m.UbicacionOrigen).Distinct().Where(u => !string.IsNullOrEmpty(u)).ToList();
            ViewBag.UbicacionesDestino = movimientos.Select(m => m.UbicacionDestino).Distinct().Where(u => !string.IsNullOrEmpty(u)).ToList();

            return View(movimientosPaginados);
        }




        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Movimiento movimiento = id.HasValue && id > 0
                ? await _movimientoManager.ObtenerPorId(id.Value)
                : new Movimiento { Fecha = DateTime.Now };

            return PartialView("_EditCreatePartial", movimiento);
        }

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
                        bulto.UbicacionActual = movimiento.UbicacionDestino; // Actualizar la ubicación del bulto
                        await _bultoManager.Actualizar(bulto);
                    }
                }
                else
                {
                    var movimientoExistente = await _movimientoManager.ObtenerPorId(movimiento.Id);
                    if (movimientoExistente == null) return NotFound();

                    movimientoExistente.Fecha = movimiento.Fecha;
                    movimientoExistente.UsuarioId = movimiento.UsuarioId; // Convertimos a string
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

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var movimiento = await _movimientoManager.ObtenerPorId(id);
                if (movimiento == null) return NotFound();

                await _movimientoManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el movimiento. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

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

        private async Task CargarListas()
        {
            ViewBag.Usuarios = _userManager.Users.ToList(); // Corregido
            ViewBag.Bultos = await _bultoManager.ObtenerTodos();
        }
    }
}
