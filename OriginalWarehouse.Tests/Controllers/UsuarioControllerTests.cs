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
    public class UsuarioControllerTests
    {
        private readonly Mock<UserManager<Usuario>> _userManagerMock;
        private readonly Mock<RoleManager<Rol>> _roleManagerMock;
        private readonly Mock<ICompositeViewEngine> _viewEngineMock;
        private readonly UsuarioController _controller;

        public UsuarioControllerTests()
        {
            var userStoreMock = new Mock<IUserStore<Usuario>>();
            _userManagerMock = new Mock<UserManager<Usuario>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null
            );

            var roleStoreMock = new Mock<IRoleStore<Rol>>();
            _roleManagerMock = new Mock<RoleManager<Rol>>(
                roleStoreMock.Object, null, null, null, null
            );

            _viewEngineMock = new Mock<ICompositeViewEngine>();

            _controller = new UsuarioController(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _viewEngineMock.Object
            );
        }

        // ✅ PRUEBA: Index debe devolver una vista con la lista de usuarios
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfUsuarios()
        {
            // Arrange
            var usuarios = new List<Usuario>
            {
                new Usuario { Id = 1, UserName = "admin", Email = "admin@correo.com" },
                new Usuario { Id = 2, UserName = "user", Email = "user@correo.com" }
            };

            _userManagerMock.Setup(m => m.Users).Returns(usuarios.AsQueryable());

            _userManagerMock
                .Setup(m => m.GetRolesAsync(It.IsAny<Usuario>()))
                .ReturnsAsync(new List<string> { "Admin" });

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Usuario>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        // ✅ PRUEBA: Guardar un nuevo usuario
        [Fact]
        public async Task Save_CreatesNewUsuario_WhenIdIsZero()
        {
            // Arrange
            var nuevoUsuario = new Usuario { Id = 0, UserName = "nuevoUsuario", Email = "nuevo@correo.com" };

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<Usuario>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Save(0, "nuevoUsuario", "nuevo@correo.com", "Password123!", 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Usuario guardado correctamente."));
        }

        // ✅ PRUEBA: Actualizar un usuario existente
        [Fact]
        public async Task Save_UpdatesExistingUsuario_WhenIdIsNotZero()
        {
            // Arrange
            var usuarioExistente = new Usuario { Id = 1, UserName = "admin", Email = "admin@correo.com" };

            _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(usuarioExistente);
            _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<Usuario>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Save(1, "adminActualizado", "admin@correo.com", null, null);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Usuario guardado correctamente."));
        }

        // ✅ PRUEBA: Eliminar un usuario existente
        [Fact]
        public async Task Delete_ReturnsJsonResult_WhenUsuarioIsDeleted()
        {
            // Arrange
            var usuario = new Usuario { Id = 1, UserName = "admin", Email = "admin@correo.com" };

            _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(usuario);
            _userManagerMock.Setup(m => m.DeleteAsync(usuario)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.True(jsonResult.Value.ToString().Contains("Usuario eliminado correctamente."));
        }
    }
}
