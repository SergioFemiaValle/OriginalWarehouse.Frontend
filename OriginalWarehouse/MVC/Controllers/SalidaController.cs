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
    public class SalidaController : Controller
    {
        private readonly ISalidaManager _salidaManager;
        private readonly IBultoManager _bultoManager;
        private readonly UserManager<Usuario> _userManager; // Usamos Identity
        private readonly ICompositeViewEngine _viewEngine;

        public SalidaController(ISalidaManager salidaManager, IBultoManager bultoManager, UserManager<Usuario> userManager, ICompositeViewEngine viewEngine)
        {
            _salidaManager = salidaManager;
            _bultoManager = bultoManager;
            _userManager = userManager; // Agregado
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string usuario = "", string bulto = "")
        {
            var salidas = await _salidaManager.ObtenerTodas();

            // 🔹 Filtrado por Usuario y Bulto
            if (!string.IsNullOrEmpty(usuario))
            {
                salidas = salidas.Where(s => s.Usuario != null && s.Usuario.UserName.Contains(usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(bulto))
            {
                salidas = salidas.Where(s => s.Bulto != null && s.Bulto.Descripcion.Contains(bulto, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRegistros = salidas.Count();

            // 🔹 Paginación
            var salidasPaginadas = salidas
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.UsuarioFiltro = usuario;
            ViewBag.BultoFiltro = bulto;

            return View(salidasPaginadas);
        }


        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Salida salida = id.HasValue && id > 0
                ? await _salidaManager.ObtenerPorId(id.Value)
                : new Salida { Fecha = DateTime.Now };

            return PartialView("_EditCreatePartial", salida);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Salida salida)
        {
            ModelState.Remove(nameof(salida.Usuario));
            ModelState.Remove(nameof(salida.Bulto));

            if (ModelState.IsValid)
            {
                if (salida.Id == 0)
                {
                    await _salidaManager.Crear(salida);
                }
                else
                {
                    var salidaExistente = await _salidaManager.ObtenerPorId(salida.Id);
                    if (salidaExistente == null) return NotFound();

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

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var salida = await _salidaManager.ObtenerPorId(id);
                if (salida == null) return NotFound();

                await _salidaManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar la salida. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

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
                    worksheet.Cells[row, 3].Value = salida.Usuario?.UserName ?? "N/A"; // Corregido
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
