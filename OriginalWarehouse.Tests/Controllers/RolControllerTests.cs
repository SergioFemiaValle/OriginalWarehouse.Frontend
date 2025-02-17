using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class RolControllerTests
    {
        private readonly Mock<RoleManager<Rol>> _roleManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly RolController _controller;

        public RolControllerTests()
        {
            var roleStoreMock = new Mock<IRoleStore<Rol>>();
            _roleManagerMock = new Mock<RoleManager<Rol>>(roleStoreMock.Object, null, null, null, null);
            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new RolController(_roleManagerMock.Object, _viewEngineMock.Object);
        }

        // ✅ PRUEBA: Index debe devolver una vista con la lista de roles
        [Fact]
        public void Index_ReturnsViewResult_WithListOfRoles()
        {
            // Arrange
            var roles = new List<Rol>
            {
                new Rol { Id = 1, Name = "Admin" },
                new Rol { Id = 2, Name = "User" }
            };

            _roleManagerMock.Setup(m => m.Roles).Returns(roles.AsQueryable());

            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Rol>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Guardar un nuevo rol
        [Fact]
        public async Task Save_CreatesNewRol_WhenIdIsZero()
        {
            // Arrange
            var nuevoRol = new Rol { Id = 0, Name = "Supervisor" };

            _roleManagerMock.Setup(m => m.CreateAsync(It.IsAny<Rol>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Save(nuevoRol);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Rol guardado correctamente."));
        }

        // ✅ PRUEBA: Actualizar un rol existente
        [Fact]
        public async Task Save_UpdatesExistingRol_WhenIdIsNotZero()
        {
            // Arrange
            var rolExistente = new Rol { Id = 1, Name = "Admin" };

            _roleManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(rolExistente);
            _roleManagerMock.Setup(m => m.UpdateAsync(It.IsAny<Rol>())).ReturnsAsync(IdentityResult.Success);

            var rolModificado = new Rol { Id = 1, Name = "SuperAdmin" };

            // Act
            var result = await _controller.Save(rolModificado);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Rol guardado correctamente."));
        }

        // ✅ PRUEBA: Eliminar un rol existente
        [Fact]
        public async Task Delete_ReturnsJsonSuccess_WhenRolIsDeleted()
        {
            // Arrange
            var rol = new Rol { Id = 1, Name = "Admin" };

            _roleManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(rol);
            _roleManagerMock.Setup(m => m.DeleteAsync(It.IsAny<Rol>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Rol eliminado correctamente."));
        }
    }
}
