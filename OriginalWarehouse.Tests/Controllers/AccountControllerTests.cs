using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Web.MVC.Controllers;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<SignInManager<Usuario>> _mockSignInManager;
        private readonly Mock<UserManager<Usuario>> _mockUserManager;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _mockSignInManager = MockSignInManager();
            _mockUserManager = MockUserManager();
            _controller = new AccountController(_mockSignInManager.Object, _mockUserManager.Object);
        }

        // ✅ PRUEBA: Debería devolver la vista Login cuando se accede por GET
        [Fact]
        public void Login_Get_DeberiaDevolverVista()
        {
            // Act
            var result = _controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model); // No hay modelo en la vista
        }

        // ✅ PRUEBA: Debería iniciar sesión correctamente y redirigir al MainMenu
        [Fact]
        public async Task Login_Post_CredencialesCorrectas_DeberiaRedirigirAMainMenu()
        {
            // Arrange
            var username = "admin@correo.com";
            var password = "Admin123*";

            _mockSignInManager
                .Setup(s => s.PasswordSignInAsync(username, password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(username, password);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("MainMenu", redirectResult.ControllerName);
        }

        // ✅ PRUEBA: Debería fallar el login con credenciales incorrectas
        [Fact]
        public async Task Login_Post_CredencialesIncorrectas_DeberiaDevolverVistaConError()
        {
            // Arrange
            var username = "usuario@correo.com";
            var password = "Incorrecta123";

            _mockSignInManager
                .Setup(s => s.PasswordSignInAsync(username, password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(username, password);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(viewResult.ViewData.ContainsKey("Error"));
        }

        // ✅ PRUEBA: Debería cerrar sesión correctamente y redirigir al login
        [Fact]
        public async Task Logout_DeberiaCerrarSesionYRedirigirALogin()
        {
            // Arrange
            _mockSignInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        // 🔹 MÉTODO AUXILIAR: Mock para SignInManager
        private Mock<SignInManager<Usuario>> MockSignInManager()
        {
            var userStoreMock = new Mock<IUserStore<Usuario>>();
            return new Mock<SignInManager<Usuario>>(
                MockUserManager().Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<Usuario>>().Object,
                null, null, null, null);
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