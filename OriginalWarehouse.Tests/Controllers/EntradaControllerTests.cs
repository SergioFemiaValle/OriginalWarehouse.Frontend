using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using OfficeOpenXml;
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
    public class EntradaControllerTests
    {
        private readonly Mock<IEntradaManager> _entradaManagerMock;
        private readonly Mock<UserManager<Usuario>> _userManagerMock;
        private readonly Mock<IBultoManager> _bultoManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly EntradaController _controller;

        public EntradaControllerTests()
        {
            _entradaManagerMock = new Mock<IEntradaManager>();
            _userManagerMock = MockUserManager();
            _bultoManagerMock = new Mock<IBultoManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new EntradaController(
                _entradaManagerMock.Object,
                _userManagerMock.Object,
                _bultoManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con una lista de entradas
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfEntradas()
        {
            // Arrange
            var entradas = new List<Entrada>
            {
                new Entrada { Id = 1, Fecha = DateTime.Now, UsuarioId = 1, BultoId = 1 },
                new Entrada { Id = 2, Fecha = DateTime.Now, UsuarioId = 2, BultoId = 2 }
            };

            _entradaManagerMock.Setup(m => m.ObtenerTodas()).ReturnsAsync(entradas);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Entrada>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Crear una nueva entrada
        [Fact]
        public async Task Save_CreatesNewEntrada_WhenIdIsZero()
        {
            // Arrange
            var nuevaEntrada = new Entrada { Id = 0, Fecha = DateTime.Now, UsuarioId = 1, BultoId = 1 };

            _entradaManagerMock.Setup(m => m.Crear(It.IsAny<Entrada>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevaEntrada);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Entrada guardada correctamente."));
        }

        // ✅ PRUEBA: Actualizar una entrada existente
        [Fact]
        public async Task Save_UpdatesExistingEntrada_WhenIdIsNotZero()
        {
            // Arrange
            var entradaExistente = new Entrada { Id = 1, Fecha = DateTime.Now, UsuarioId = 1, BultoId = 1 };

            _entradaManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(entradaExistente);
            _entradaManagerMock.Setup(m => m.Actualizar(It.IsAny<Entrada>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(entradaExistente);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Entrada guardada correctamente."));
        }

        // ✅ PRUEBA: Eliminar una entrada existente
        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenEntradaIsDeleted()
        {
            // Arrange
            var entradaId = 1;
            var entrada = new Entrada { Id = entradaId, Fecha = DateTime.Now };

            _entradaManagerMock.Setup(m => m.ObtenerPorId(entradaId)).ReturnsAsync(entrada);
            _entradaManagerMock.Setup(m => m.Eliminar(entradaId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(entradaId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        // ✅ PRUEBA: Exportar Excel con las entradas
        [Fact]
        public async Task ExportarExcel_ReturnsFileResult_WithEntradasData()
        {
            // Arrange
            var entradas = new List<Entrada>
            {
                new Entrada { Id = 1, Fecha = DateTime.Now },
                new Entrada { Id = 2, Fecha = DateTime.Now }
            };

            _entradaManagerMock.Setup(m => m.ObtenerTodas()).ReturnsAsync(entradas);

            // Act
            var result = await _controller.ExportarExcel();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.NotNull(fileResult);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        }

        // 🔹 MÉTODO AUXILIAR: Mock para UserManager
        private Mock<UserManager<Usuario>> MockUserManager()
        {
            var userStoreMock = new Mock<IUserStore<Usuario>>();
            return new Mock<UserManager<Usuario>>(
                userStoreMock.Object,
                null, null, null, null, null, null, null, null);
        }
    }
}
