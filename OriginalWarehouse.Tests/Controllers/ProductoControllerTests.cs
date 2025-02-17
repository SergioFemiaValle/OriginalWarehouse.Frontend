using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using OfficeOpenXml;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class ProductoControllerTests
    {
        private readonly Mock<IProductoManager> _productoManagerMock;
        private readonly Mock<ICategoriaProductoManager> _categoriaProductoManagerMock;
        private readonly Mock<IAlmacenamientoEspecialManager> _almacenamientoEspecialManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly ProductoController _controller;

        public ProductoControllerTests()
        {
            // Configurando los mocks
            _productoManagerMock = new Mock<IProductoManager>();
            _categoriaProductoManagerMock = new Mock<ICategoriaProductoManager>();
            _almacenamientoEspecialManagerMock = new Mock<IAlmacenamientoEspecialManager>();
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            // Inicializando el controlador con los mocks
            _controller = new ProductoController(
                _productoManagerMock.Object,
                _categoriaProductoManagerMock.Object,
                _almacenamientoEspecialManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        [Fact]
        public async Task IndexAsync_ReturnsViewResult_WithListOfProductos()
        {
            // Arrange: Preparar datos para el test
            var productos = new List<Producto>
            {
                new Producto { Id = 1, Nombre = "Producto A", CantidadEnStock = 10, Precio = 5.99m },
                new Producto { Id = 2, Nombre = "Producto B", CantidadEnStock = 5, Precio = 10.99m }
            };

            // Configurando el mock para que devuelva productos
            _productoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(productos);

            // Act: Ejecutar el método que se está probando
            var result = await _controller.IndexAsync();

            // Assert: Verificar que se retorna un ViewResult y los datos son correctos
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Producto>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Save_CreatesNewProducto_WhenIdIsZero()
        {
            // Arrange
            var nuevoProducto = new Producto { Id = 0, Nombre = "Producto C", Precio = 15.99m, CantidadEnStock = 20 };

            _productoManagerMock.Setup(m => m.Crear(It.IsAny<Producto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(nuevoProducto);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Producto guardado correctamente."));
        }

        [Fact]
        public async Task Save_UpdatesExistingProducto_WhenIdIsNotZero()
        {
            // Arrange
            var productoExistente = new Producto { Id = 1, Nombre = "Producto A", Precio = 5.99m, CantidadEnStock = 10 };
            _productoManagerMock.Setup(m => m.ObtenerPorId(1)).ReturnsAsync(productoExistente);
            _productoManagerMock.Setup(m => m.Actualizar(It.IsAny<Producto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Save(productoExistente);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Producto guardado correctamente."));
        }

        [Fact]
        public async Task Delete_ReturnsRedirectResult_WhenProductoIsDeleted()
        {
            // Arrange
            var productoId = 1;
            var producto = new Producto { Id = productoId, Nombre = "Producto A", Precio = 5.99m, CantidadEnStock = 10 };
            _productoManagerMock.Setup(m => m.ObtenerPorId(productoId)).ReturnsAsync(producto);
            _productoManagerMock.Setup(m => m.Eliminar(productoId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(productoId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task ExportarExcel_ReturnsFileResult_WithProductsData()
        {
            // Arrange
            var productos = new List<Producto>
            {
                new Producto { Id = 1, Nombre = "Producto A", Precio = 5.99m, CantidadEnStock = 10 },
                new Producto { Id = 2, Nombre = "Producto B", Precio = 15.99m, CantidadEnStock = 5 }
            };

            _productoManagerMock.Setup(m => m.ObtenerTodos()).ReturnsAsync(productos);

            // Act
            var result = await _controller.ExportarExcel();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.NotNull(fileResult);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        }
    }
}
