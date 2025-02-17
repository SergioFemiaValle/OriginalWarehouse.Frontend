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
    public class EntradaController : Controller
    {
        private readonly IEntradaManager _entradaManager;
        private readonly UserManager<Usuario> _userManager; // Usamos Identity
        private readonly IBultoManager _bultoManager;
        private readonly ICompositeViewEngine _viewEngine;

        public EntradaController(IEntradaManager entradaManager, UserManager<Usuario> userManager, IBultoManager bultoManager, ICompositeViewEngine viewEngine)
        {
            _entradaManager = entradaManager;
            _userManager = userManager; // Agregado
            _bultoManager = bultoManager;
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string usuario = "", string bulto = "")
        {
            var entradas = await _entradaManager.ObtenerTodas();

            // 🔹 Filtrado por Usuario y Bulto
            if (!string.IsNullOrEmpty(usuario))
            {
                entradas = entradas.Where(e => e.Usuario != null && e.Usuario.UserName.Contains(usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(bulto))
            {
                entradas = entradas.Where(e => e.Bulto != null && e.Bulto.Descripcion.Contains(bulto, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRegistros = entradas.Count();

            // 🔹 Paginación
            var entradasPaginadas = entradas
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.UsuarioFiltro = usuario;
            ViewBag.BultoFiltro = bulto;

            return View(entradasPaginadas);
        }


        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Entrada entrada = id.HasValue && id > 0
                ? await _entradaManager.ObtenerPorId(id.Value)
                : new Entrada { Fecha = DateTime.Now };

            return PartialView("_EditCreatePartial", entrada);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Entrada entrada)
        {
            ModelState.Remove(nameof(entrada.Usuario));
            ModelState.Remove(nameof(entrada.Bulto));

            if (ModelState.IsValid)
            {
                if (entrada.Id == 0)
                {
                    await _entradaManager.Crear(entrada);
                }
                else
                {
                    var entradaExistente = await _entradaManager.ObtenerPorId(entrada.Id);
                    if (entradaExistente == null) return NotFound();

                    entradaExistente.Fecha = entrada.Fecha;
                    entradaExistente.UsuarioId = entrada.UsuarioId; // Convertimos a string
                    entradaExistente.BultoId = entrada.BultoId;

                    await _entradaManager.Actualizar(entradaExistente);
                }

                return Json(new { success = true, message = "Entrada guardada correctamente." });
            }

            await CargarListas();
            var html = await RenderPartialViewToString("_EditCreatePartial", entrada);
            return Json(new { success = false, html });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entrada = await _entradaManager.ObtenerPorId(id);
                if (entrada == null) return NotFound();

                await _entradaManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar la entrada. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

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
