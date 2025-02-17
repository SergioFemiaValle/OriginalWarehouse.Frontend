using Microsoft.AspNetCore.Identity;
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
    public class MovimientoControllerTests
    {
        private readonly Mock<IMovimientoManager> _movimientoManagerMock;
        private readonly Mock<UserManager<Usuario>> _userManagerMock;
        private readonly Mock<IBultoManager> _bultoManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly MovimientoController _controller;

        public MovimientoControllerTests()
        {
            _movimientoManagerMock = new Mock<IMovimientoManager>();
            _userManagerMock = new Mock<UserManager<Usuario>>(
                new Mock<IUserStore<Usuario>>().Object, null, null, null, null, null, null, null, null);
            _bultoManagerMock = new Mock<IBultoManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new MovimientoController(
                _movimientoManagerMock.Object,
                _userManagerMock.Object,
                _bultoManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con una lista de movimientos
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfMovimientos()
        {
            // Arrange
            var movimientos = new List<Movimiento>
            {
                new Movimiento { Id = 1, UbicacionOrigen = "A1", UbicacionDestino = "B2" },
                new Movimiento { Id = 2, UbicacionOrigen = "C3", UbicacionDestino = "D4" }
            };

            _movimientoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(movimientos);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Movimiento>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Guardar un nuevo movimiento
        [Fact]
        public async Task Save_CreatesNewMovimiento_WhenIdIsZero()
        {
            // Arrange
            var nuevoMovimiento = new Movimiento { Id = 0, UbicacionOrigen = "A1", UbicacionDestino = "B2" };

            _movimientoManagerMock.Setup(m => m.Crear(It.IsAny<Movimiento>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevoMovimiento);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Movimiento guardado correctamente."));
        }

        // ✅ PRUEBA: Actualizar un movimiento existente
        [Fact]
        public async Task Save_UpdatesExistingMovimiento_WhenIdIsNotZero()
        {
            // Arrange
            var movimientoExistente = new Movimiento { Id = 1, UbicacionOrigen = "A1", UbicacionDestino = "B2" };

            _movimientoManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(movimientoExistente);
            _movimientoManagerMock.Setup(m => m.Actualizar(It.IsAny<Movimiento>())).Returns(Task.CompletedTask);

            var movimientoModificado = new Movimiento { Id = 1, UbicacionOrigen = "C3", UbicacionDestino = "D4" };

            // Act
            var result = await _controller.Save(movimientoModificado);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Movimiento guardado correctamente."));
        }

        // ✅ PRUEBA: Eliminar un movimiento existente
        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenMovimientoIsDeleted()
        {
            // Arrange
            var movimientoId = 1;
            var movimiento = new Movimiento { Id = movimientoId, UbicacionOrigen = "A1", UbicacionDestino = "B2" };

            _movimientoManagerMock.Setup(m => m.ObtenerPorId(movimientoId)).ReturnsAsync(movimiento);
            _movimientoManagerMock.Setup(m => m.Eliminar(movimientoId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(movimientoId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}
