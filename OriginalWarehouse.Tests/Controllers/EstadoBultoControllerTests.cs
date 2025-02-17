using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class EstadoBultoControllerTests
    {
        private readonly Mock<IEstadoBultoManager> _estadoBultoManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly EstadoBultoController _controller;

        public EstadoBultoControllerTests()
        {
            _estadoBultoManagerMock = new Mock<IEstadoBultoManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new EstadoBultoController(
                _estadoBultoManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con una lista de estados de bulto
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfEstados()
        {
            // Arrange
            var estados = new List<EstadoBulto>
            {
                new EstadoBulto { Id = 1, Nombre = "Disponible" },
                new EstadoBulto { Id = 2, Nombre = "En tránsito" }
            };

            _estadoBultoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(estados);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<EstadoBulto>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Guardar un nuevo estado de bulto
        [Fact]
        public async Task Save_CreatesNewEstado_WhenIdIsZero()
        {
            // Arrange
            var nuevoEstado = new EstadoBulto { Id = 0, Nombre = "Nuevo Estado" };

            _estadoBultoManagerMock.Setup(m => m.Crear(It.IsAny<EstadoBulto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevoEstado);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Estado de bulto guardado correctamente."));
        }

        // ✅ PRUEBA: Actualizar un estado de bulto existente
        [Fact]
        public async Task Save_UpdatesExistingEstado_WhenIdIsNotZero()
        {
            // Arrange
            var estadoExistente = new EstadoBulto { Id = 1, Nombre = "Antiguo Estado" };

            _estadoBultoManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(estadoExistente);
            _estadoBultoManagerMock.Setup(m => m.Actualizar(It.IsAny<EstadoBulto>())).Returns(Task.CompletedTask);

            var estadoModificado = new EstadoBulto { Id = 1, Nombre = "Estado Modificado" };

            // Act
            var result = await _controller.Save(estadoModificado);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Estado de bulto guardado correctamente."));
        }

        // ✅ PRUEBA: Eliminar un estado de bulto existente
        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenEstadoIsDeleted()
        {
            // Arrange
            var estadoId = 1;
            var estado = new EstadoBulto { Id = estadoId, Nombre = "Estado A" };

            _estadoBultoManagerMock.Setup(m => m.ObtenerPorId(estadoId)).ReturnsAsync(estado);
            _estadoBultoManagerMock.Setup(m => m.Eliminar(estadoId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(estadoId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}
