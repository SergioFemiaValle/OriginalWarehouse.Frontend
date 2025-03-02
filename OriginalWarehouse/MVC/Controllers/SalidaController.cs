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
        public SalidaController(ISalidaManager salidaManager, IBultoManager bultoManager, UserManager<Usuario> userManager, ICompositeViewEngine viewEngine)
        {
            _salidaManager = salidaManager;
            _bultoManager = bultoManager;
            _userManager = userManager;
            _viewEngine = viewEngine;
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
                if (salida.Id == 0)
                {
                    var detalles = (await _detalleBultoManager.ObtenerTodos()).Where(d => d.BultoId == salida.BultoId);

                    foreach (var detalle in detalles)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null && producto.CantidadEnStock < detalle.Cantidad)
                        {
                            return Json(new { success = false, message = $"No hay suficiente stock para el producto {producto.Nombre}. Salida cancelada." });
                        }
                    }

                    await _salidaManager.Crear(salida);

                    foreach (var detalle in detalles)
                    {
                        var producto = await _productoManager.ObtenerPorId(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.CantidadEnStock -= detalle.Cantidad;
                            await _productoManager.Actualizar(producto);
                        }
                    }
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
            ViewBag.Bultos = await _bultoManager.ObtenerTodos();
        }
    }
}
