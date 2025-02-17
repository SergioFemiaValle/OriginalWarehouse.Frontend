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
    public class BultoControllerTests
    {
        private readonly Mock<IBultoManager> _bultoManagerMock;
        private readonly Mock<IEstadoBultoManager> _estadoBultoManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly BultoController _controller;

        public BultoControllerTests()
        {
            _bultoManagerMock = new Mock<IBultoManager>();
            _estadoBultoManagerMock = new Mock<IEstadoBultoManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new BultoController(
                _bultoManagerMock.Object,
                _estadoBultoManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con una lista de bultos
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfBultos()
        {
            // Arrange
            var bultos = new List<Bulto>
            {
                new Bulto { Id = 1, Descripcion = "Bulto A", UbicacionActual = "Zona 1" },
                new Bulto { Id = 2, Descripcion = "Bulto B", UbicacionActual = "Zona 2" }
            };

            _bultoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(bultos);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Bulto>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Crear un nuevo bulto
        [Fact]
        public async Task Save_CreatesNewBulto_WhenIdIsZero()
        {
            // Arrange
            var nuevoBulto = new Bulto { Id = 0, Descripcion = "Bulto Nuevo", UbicacionActual = "Zona X" };

            _bultoManagerMock.Setup(m => m.Crear(It.IsAny<Bulto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevoBulto);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Bulto guardado correctamente."));
        }

        // ✅ PRUEBA: Actualizar un bulto existente
        [Fact]
        public async Task Save_UpdatesExistingBulto_WhenIdIsNotZero()
        {
            // Arrange
            var bultoExistente = new Bulto { Id = 1, Descripcion = "Bulto Viejo", UbicacionActual = "Zona Y" };

            _bultoManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(bultoExistente);
            _bultoManagerMock.Setup(m => m.Actualizar(It.IsAny<Bulto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(bultoExistente);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Bulto guardado correctamente."));
        }

        // ✅ PRUEBA: Eliminar un bulto existente
        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenBultoIsDeleted()
        {
            // Arrange
            var bultoId = 1;
            var bulto = new Bulto { Id = bultoId, Descripcion = "Bulto A", UbicacionActual = "Zona 1" };

            _bultoManagerMock.Setup(m => m.ObtenerPorId(bultoId)).ReturnsAsync(bulto);
            _bultoManagerMock.Setup(m => m.Eliminar(bultoId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(bultoId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        // ✅ PRUEBA: Exportar a Excel
        [Fact]
        public async Task ExportarExcel_ReturnsFileResult_WithBultosData()
        {
            // Arrange
            var bultos = new List<Bulto>
            {
                new Bulto { Id = 1, Descripcion = "Bulto A", UbicacionActual = "Zona 1" },
                new Bulto { Id = 2, Descripcion = "Bulto B", UbicacionActual = "Zona 2" }
            };

            _bultoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(bultos);

            // Act
            var result = await _controller.ExportarExcel();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.NotNull(fileResult);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        }
    }
}
