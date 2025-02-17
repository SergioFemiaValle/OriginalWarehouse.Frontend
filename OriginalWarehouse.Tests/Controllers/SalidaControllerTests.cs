using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class SalidaControllerTests
    {
        private readonly Mock<ISalidaManager> _salidaManagerMock;
        private readonly Mock<IBultoManager> _bultoManagerMock;
        private readonly Mock<UserManager<Usuario>> _userManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly SalidaController _controller;

        public SalidaControllerTests()
        {
            _salidaManagerMock = new Mock<ISalidaManager>();
            _bultoManagerMock = new Mock<IBultoManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            var userStoreMock = new Mock<IUserStore<Usuario>>();
            _userManagerMock = new Mock<UserManager<Usuario>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null
            );

            _controller = new SalidaController(
                _salidaManagerMock.Object,
                _bultoManagerMock.Object,
                _userManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con la lista de salidas
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfSalidas()
        {
            // Arrange
            var salidas = new List<Salida>
            {
                new Salida { Id = 1, Fecha = DateTime.Now, UsuarioId = 1, BultoId = 1 },
                new Salida { Id = 2, Fecha = DateTime.Now.AddDays(-1), UsuarioId = 2, BultoId = 2 }
            };

            _salidaManagerMock.Setup(m => m.ObtenerTodas()).ReturnsAsync(salidas);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Salida>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Guardar una nueva salida
        [Fact]
        public async Task Save_CreatesNewSalida_WhenIdIsZero()
        {
            // Arrange
            var nuevaSalida = new Salida { Id = 0, Fecha = DateTime.Now, UsuarioId = 1, BultoId = 1 };

            _salidaManagerMock.Setup(m => m.Crear(It.IsAny<Salida>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevaSalida);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Salida guardada correctamente."));
        }

        // ✅ PRUEBA: Actualizar una salida existente
        [Fact]
        public async Task Save_UpdatesExistingSalida_WhenIdIsNotZero()
        {
            // Arrange
            var salidaExistente = new Salida { Id = 1, Fecha = DateTime.Now, UsuarioId = 1, BultoId = 1 };

            _salidaManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(salidaExistente);
            _salidaManagerMock.Setup(m => m.Actualizar(It.IsAny<Salida>())).Returns(Task.CompletedTask);

            var salidaModificada = new Salida { Id = 1, Fecha = DateTime.Now.AddDays(1), UsuarioId = 2, BultoId = 2 };

            // Act
            var result = await _controller.Save(salidaModificada);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Salida guardada correctamente."));
        }

        // ✅ PRUEBA: Eliminar una salida existente
        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenSalidaIsDeleted()
        {
            // Arrange
            var salida = new Salida { Id = 1, Fecha = DateTime.Now, UsuarioId = 1, BultoId = 1 };

            _salidaManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(salida);
            _salidaManagerMock.Setup(m => m.Eliminar(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}
