using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using OfficeOpenXml;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class DetalleBultoControllerTests
    {
        private readonly Mock<IDetalleBultoManager> _detalleBultoManagerMock;
        private readonly Mock<IProductoManager> _productoManagerMock;
        private readonly Mock<IBultoManager> _bultoManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly DetalleBultoController _controller;

        public DetalleBultoControllerTests()
        {
            _detalleBultoManagerMock = new Mock<IDetalleBultoManager>();
            _productoManagerMock = new Mock<IProductoManager>();
            _bultoManagerMock = new Mock<IBultoManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new DetalleBultoController(
                _detalleBultoManagerMock.Object,
                _productoManagerMock.Object,
                _bultoManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con una lista de detalles de bulto
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfDetallesBulto()
        {
            // Arrange
            var detalles = new List<DetalleBulto>
            {
                new DetalleBulto { Id = 1, Cantidad = 10, Lote = "Lote1" },
                new DetalleBulto { Id = 2, Cantidad = 5, Lote = "Lote2" }
            };

            _detalleBultoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(detalles);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<DetalleBulto>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Crear un nuevo detalle de bulto
        [Fact]
        public async Task Save_CreatesNewDetalleBulto_WhenIdIsZero()
        {
            // Arrange
            var nuevoDetalle = new DetalleBulto { Id = 0, Cantidad = 10, Lote = "NuevoLote" };

            _detalleBultoManagerMock.Setup(m => m.Crear(It.IsAny<DetalleBulto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevoDetalle);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Detalle de Bulto guardado correctamente."));
        }

        // ✅ PRUEBA: Actualizar un detalle de bulto existente
        [Fact]
        public async Task Save_UpdatesExistingDetalleBulto_WhenIdIsNotZero()
        {
            // Arrange
            var detalleExistente = new DetalleBulto { Id = 1, Cantidad = 10, Lote = "LoteViejo" };

            _detalleBultoManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(detalleExistente);
            _detalleBultoManagerMock.Setup(m => m.Actualizar(It.IsAny<DetalleBulto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(detalleExistente);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Detalle de Bulto guardado correctamente."));
        }

        // ✅ PRUEBA: Eliminar un detalle de bulto existente
        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenDetalleBultoIsDeleted()
        {
            // Arrange
            var detalleId = 1;
            var detalle = new DetalleBulto { Id = detalleId, Cantidad = 10, Lote = "LoteX" };

            _detalleBultoManagerMock.Setup(m => m.ObtenerPorId(detalleId)).ReturnsAsync(detalle);
            _detalleBultoManagerMock.Setup(m => m.Eliminar(detalleId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(detalleId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        // ✅ PRUEBA: Exportar Excel con los detalles de bulto
        [Fact]
        public async Task ExportarExcel_ReturnsFileResult_WithDetalleBultoData()
        {
            // Arrange
            var detalles = new List<DetalleBulto>
            {
                new DetalleBulto { Id = 1, Cantidad = 10, Lote = "LoteA" },
                new DetalleBulto { Id = 2, Cantidad = 5, Lote = "LoteB" }
            };

            _detalleBultoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(detalles);

            // Act
            var result = await _controller.ExportarExcel();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.NotNull(fileResult);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        }
    }
}
