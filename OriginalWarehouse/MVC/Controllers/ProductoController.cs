﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OfficeOpenXml;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Controllers
{
    /// <summary>
    /// Controlador para la gestión de productos.
    /// Permite listar, filtrar, crear, editar, eliminar y exportar productos en formato Excel.
    /// </summary>
    public class ProductoController : Controller
    {
        private readonly IProductoManager _productoManager;
        private readonly ICategoriaProductoManager _categoriaProductoManager;
        private readonly IAlmacenamientoEspecialManager _almacenamientoEspecialManager;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly IDetalleBultoManager _detallesBultoManager;

        /// <summary>
        /// Constructor del controlador de productos.
        /// </summary>
        public ProductoController(IProductoManager productoManager, ICategoriaProductoManager categoriaProductoManager,
            IAlmacenamientoEspecialManager almacenamientoEspecialManager,
            ICompositeViewEngine viewEngine, IDetalleBultoManager detallesBultoManager)
        {
            _productoManager = productoManager;
            _categoriaProductoManager = categoriaProductoManager;
            _almacenamientoEspecialManager = almacenamientoEspecialManager;
            _viewEngine = viewEngine;
            _detallesBultoManager = detallesBultoManager;
        }

        /// <summary>
        /// Muestra la lista de productos con filtros y paginación.
        /// </summary>
        public async Task<IActionResult> IndexAsync(string searchNombre, string searchCategoria, int page = 1, int pageSize = 10)
        {
            var productos = await _productoManager.ObtenerTodos();

            if (!string.IsNullOrEmpty(searchNombre))
            {
                productos = productos.Where(p => p.Nombre.Contains(searchNombre, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(searchCategoria))
            {
                productos = productos.Where(p => p.Categoria?.Nombre == searchCategoria).ToList();
            }

            int totalRegistros = productos.Count();
            var productosPaginados = productos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.SearchNombre = searchNombre;
            ViewBag.SearchCategoria = searchCategoria;

            return View(productosPaginados);
        }

        /// <summary>
        /// Muestra la vista parcial para la creación o edición de un producto.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditPartial(int? id)
        {
            await CargarListas();

            Producto producto = id.HasValue && id > 0
                ? await _productoManager.ObtenerPorId(id.Value) ?? new Producto()
                : new Producto();

            return PartialView("_EditCreatePartial", producto);
        }

        /// <summary>
        /// Guarda un nuevo producto o actualiza uno existente.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Producto producto)
        {
            ModelState.Remove(nameof(producto.Categoria));
            ModelState.Remove(nameof(producto.AlmacenamientoEspecial));
            ModelState.Remove(nameof(producto.Detalles));

            if (ModelState.IsValid)
            {
                if (producto.Id == 0)
                {
                    await _productoManager.Crear(producto);
                }
                else
                {
                    var productoExistente = await _productoManager.ObtenerPorId(producto.Id);
                    if (productoExistente == null) return NotFound();

                    productoExistente.Nombre = producto.Nombre;
                    productoExistente.Precio = producto.Precio;
                    productoExistente.CantidadEnStock = producto.CantidadEnStock;
                    productoExistente.CategoriaId = producto.CategoriaId;
                    productoExistente.AlmacenamientoEspecialId = producto.AlmacenamientoEspecialId;

                    await _productoManager.Actualizar(productoExistente);
                }

                return Json(new { success = true, message = "Producto guardado correctamente." });
            }

            var html = await RenderPartialViewToString("_EditCreatePartial", producto);
            return Json(new { success = false, html });
        }

        /// <summary>
        /// Elimina un producto si no está relacionado con un bulto.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var producto = await _productoManager.ObtenerPorId(id);
                if (producto == null) return NotFound();

                var detalles = (await _detallesBultoManager.ObtenerTodos()).Where(d => d.ProductoId == id);
                if (detalles.Any())
                {
                    return Json(new { success = false, message = "No se puede eliminar el producto porque está asociado a un Detalle de un Bulto." });
                }

                await _productoManager.Eliminar(id);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el producto. Intente nuevamente.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Exporta la lista de productos a un archivo de Excel.
        /// </summary>
        public async Task<IActionResult> ExportarExcel()
        {
            var productos = await _productoManager.ObtenerTodos();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Productos");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nombre";
                worksheet.Cells[1, 3].Value = "Categoría";
                worksheet.Cells[1, 4].Value = "Precio (€)";
                worksheet.Cells[1, 5].Value = "Cantidad en Stock";
                worksheet.Cells[1, 6].Value = "Almacenamiento Especial";

                using (var headerRange = worksheet.Cells["A1:F1"])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                int row = 2;
                foreach (var producto in productos)
                {
                    worksheet.Cells[row, 1].Value = producto.Id;
                    worksheet.Cells[row, 2].Value = producto.Nombre;
                    worksheet.Cells[row, 3].Value = producto.Categoria?.Nombre ?? "Sin categoría";
                    worksheet.Cells[row, 4].Value = producto.Precio;
                    worksheet.Cells[row, 5].Value = producto.CantidadEnStock;
                    worksheet.Cells[row, 6].Value = producto.AlmacenamientoEspecial?.Nombre ?? "N/A";
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                var fileContent = package.GetAsByteArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Productos.xlsx");
            }
        }


        #region private methods

        /// <summary>
        /// Carga las listas necesarias para la vista de edición/creación.
        /// </summary>
        private async Task CargarListas()
        {
            ViewBag.Categorias = await _categoriaProductoManager.ObtenerTodas();
            ViewBag.AlmacenamientosEspeciales = await _almacenamientoEspecialManager.ObtenerTodos();
        }

        /// <summary>
        /// Renderiza una vista parcial como string.
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

        #endregion
    }
}
