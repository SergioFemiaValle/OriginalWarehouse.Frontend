using Microsoft.AspNetCore.Mvc;
using Moq;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class AlmacenamientoEspecialControllerTests
    {
        private readonly Mock<IAlmacenamientoEspecialManager> _mockAlmacenamientoEspecialManager;
        private readonly AlmacenamientoEspecialController _controller;

        public AlmacenamientoEspecialControllerTests()
        {
            _mockAlmacenamientoEspecialManager = new Mock<IAlmacenamientoEspecialManager>();
            _controller = new AlmacenamientoEspecialController(_mockAlmacenamientoEspecialManager.Object, null);
        }

        // ✅ PRUEBA: Verificar que la vista Index carga correctamente con datos
        [Fact]
        public async Task Index_DeberiaRetornarVistaConListaDeAlmacenamientos()
        {
            // Arrange
            var almacenamientos = new List<AlmacenamientoEspecial>
            {
                new AlmacenamientoEspecial { Id = 1, Nombre = "Almacenamiento Frío" },
                new AlmacenamientoEspecial { Id = 2, Nombre = "Almacenamiento Seco" }
            };

            _mockAlmacenamientoEspecialManager
                .Setup(m => m.ObtenerTodos())
                .ReturnsAsync(almacenamientos);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<AlmacenamientoEspecial>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        // ✅ PRUEBA: Verificar que EditPartial retorna la vista parcial correcta
        [Fact]
        public async Task EditPartial_ConIdExistente_DeberiaRetornarVistaParcialConAlmacenamiento()
        {
            // Arrange
            var almacenamiento = new AlmacenamientoEspecial { Id = 1, Nombre = "Almacenamiento Prueba" };

            _mockAlmacenamientoEspecialManager
                .Setup(m => m.ObtenerPorId(1))
                .ReturnsAsync(almacenamiento);

            // Act
            var result = _controller.EditPartial(1);

            // Assert
            var viewResult = Assert.IsType<PartialViewResult>(result);
            var model = Assert.IsType<AlmacenamientoEspecial>(viewResult.Model);
            Assert.Equal("Almacenamiento Prueba", model.Nombre);
        }

        // ✅ PRUEBA: Guardar un nuevo almacenamiento especial
        [Fact]
        public async Task Save_AlmacenamientoNuevo_DeberiaCrearYRetornarJsonExito()
        {
            // Arrange
            var nuevoAlmacenamiento = new AlmacenamientoEspecial { Id = 0, Nombre = "Nuevo Almacenamiento" };

            _mockAlmacenamientoEspecialManager
                .Setup(m => m.Crear(It.IsAny<AlmacenamientoEspecial>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevoAlmacenamiento);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Almacenamiento especial guardado correctamente."));
        }

        // ✅ PRUEBA: Editar un almacenamiento especial existente
        [Fact]
        public async Task Save_AlmacenamientoExistente_DeberiaActualizarYRetornarJsonExito()
        {
            // Arrange
            var almacenamientoExistente = new AlmacenamientoEspecial { Id = 1, Nombre = "Antiguo Nombre" };

            _mockAlmacenamientoEspecialManager
                .Setup(m => m.ObtenerPorId(1))
                .ReturnsAsync(almacenamientoExistente);

            _mockAlmacenamientoEspecialManager
                .Setup(m => m.Actualizar(It.IsAny<AlmacenamientoEspecial>()))
                .Returns(Task.CompletedTask);

            var almacenamientoModificado = new AlmacenamientoEspecial { Id = 1, Nombre = "Nuevo Nombre" };

            // Act
            var result = await _controller.Save(almacenamientoModificado);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Almacenamiento especial guardado correctamente."));
        }

        // ✅ PRUEBA: Eliminar un almacenamiento especial existente
        [Fact]
        public async Task Delete_AlmacenamientoExistente_DeberiaEliminarYRedirigir()
        {
            // Arrange
            var almacenamiento = new AlmacenamientoEspecial { Id = 1, Nombre = "Almacenamiento a eliminar" };

            _mockAlmacenamientoEspecialManager
                .Setup(m => m.ObtenerPorId(1))
                .ReturnsAsync(almacenamiento);

            _mockAlmacenamientoEspecialManager
                .Setup(m => m.Eliminar(1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        // ✅ PRUEBA: Intentar eliminar un almacenamiento que no existe
        [Fact]
        public async Task Delete_AlmacenamientoNoExistente_DeberiaRetornarNotFound()
        {
            // Arrange
            _mockAlmacenamientoEspecialManager
                .Setup(m => m.ObtenerPorId(1))
                .ReturnsAsync((AlmacenamientoEspecial)null);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
