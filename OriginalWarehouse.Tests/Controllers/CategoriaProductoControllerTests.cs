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
    public class CategoriaProductoControllerTests
    {
        private readonly Mock<ICategoriaProductoManager> _categoriaProductoManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly CategoriaProductoController _controller;

        public CategoriaProductoControllerTests()
        {
            _categoriaProductoManagerMock = new Mock<ICategoriaProductoManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new CategoriaProductoController(
                _categoriaProductoManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con una lista de categorías
        [Fact]
        public async Task IndexAsync_ReturnsViewResult_WithListOfCategorias()
        {
            // Arrange
            var categorias = new List<CategoriaProducto>
            {
                new CategoriaProducto { Id = 1, Nombre = "Categoría A" },
                new CategoriaProducto { Id = 2, Nombre = "Categoría B" }
            };

            _categoriaProductoManagerMock.Setup(m => m.ObtenerTodas()).ReturnsAsync(categorias);

            // Act
            var result = await _controller.IndexAsync();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CategoriaProducto>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Crear una nueva categoría
        [Fact]
        public async Task Save_CreatesNewCategoria_WhenIdIsZero()
        {
            // Arrange
            var nuevaCategoria = new CategoriaProducto { Id = 0, Nombre = "Nueva Categoría" };

            _categoriaProductoManagerMock.Setup(m => m.Crear(It.IsAny<CategoriaProducto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevaCategoria);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Categoría guardada correctamente."));
        }

        // ✅ PRUEBA: Actualizar una categoría existente
        [Fact]
        public async Task Save_UpdatesExistingCategoria_WhenIdIsNotZero()
        {
            // Arrange
            var categoriaExistente = new CategoriaProducto { Id = 1, Nombre = "Categoría Antigua" };

            _categoriaProductoManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(categoriaExistente);
            _categoriaProductoManagerMock.Setup(m => m.Actualizar(It.IsAny<CategoriaProducto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(categoriaExistente);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Categoría guardada correctamente."));
        }

        // ✅ PRUEBA: Eliminar una categoría existente
        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenCategoriaIsDeleted()
        {
            // Arrange
            var categoriaId = 1;
            var categoria = new CategoriaProducto { Id = categoriaId, Nombre = "Categoría A" };

            _categoriaProductoManagerMock.Setup(m => m.ObtenerPorId(categoriaId)).ReturnsAsync(categoria);
            _categoriaProductoManagerMock.Setup(m => m.Eliminar(categoriaId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(categoriaId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}
