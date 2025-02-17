using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.MVC.Models;
using OriginalWarehouse.Web.MVC.Controllers;
using System.Threading.Tasks;
using Xunit;

namespace OriginalWarehouse.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<Usuario>> _mockUserManager;
        private readonly Mock<SignInManager<Usuario>> _mockSignInManager;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockUserManager = MockUserManager();
            _mockSignInManager = MockSignInManager();
            _controller = new AuthController(_mockUserManager.Object, _mockSignInManager.Object);
        }

        // ✅ PRUEBA: Vista de Login debe retornar una vista válida
        [Fact]
        public void Login_Get_DeberiaDevolverVista()
        {
            // Act
            var result = _controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
        }

        // ✅ PRUEBA: Login exitoso debería redirigir a Home
        [Fact]
        public async Task Login_Post_CredencialesCorrectas_DeberiaRedirigirAHome()
        {
            // Arrange
            var loginModel = new LoginViewModel { Email = "usuario@correo.com", Password = "Contraseña123", RememberMe = false };

            _mockSignInManager
                .Setup(s => s.PasswordSignInAsync(loginModel.Email, loginModel.Password, loginModel.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        // ✅ PRUEBA: Login fallido debería devolver la vista con error
        [Fact]
        public async Task Login_Post_CredencialesIncorrectas_DeberiaDevolverVistaConError()
        {
            // Arrange
            var loginModel = new LoginViewModel { Email = "usuario@correo.com", Password = "Incorrecta123", RememberMe = false };

            _mockSignInManager
                .Setup(s => s.PasswordSignInAsync(loginModel.Email, loginModel.Password, loginModel.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid); ;
        }

        // ✅ PRUEBA: Vista de Registro debe retornar una vista válida
        [Fact]
        public void Register_Get_DeberiaDevolverVista()
        {
            // Act
            var result = _controller.Register();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
        }

        // ✅ PRUEBA: Registro exitoso debería redirigir a Home
        [Fact]
        public async Task Register_Post_DatosValidos_DeberiaRegistrarYRedirigirAHome()
        {
            // Arrange
            var registerModel = new RegisterViewModel
            {
                Email = "nuevo@correo.com",
                Password = "Contraseña123",
                ConfirmPassword = "Contraseña123"
            };

            var nuevoUsuario = new Usuario { UserName = registerModel.Email, Email = registerModel.Email };

            _mockUserManager
                .Setup(u => u.CreateAsync(It.IsAny<Usuario>(), registerModel.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockSignInManager
                .Setup(s => s.SignInAsync(It.IsAny<Usuario>(), false, null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Register(registerModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        // ✅ PRUEBA: Registro con datos inválidos debería devolver la vista con errores
        [Fact]
        public async Task Register_Post_DatosInvalidos_DeberiaDevolverVistaConErrores()
        {
            // Arrange
            var registerModel = new RegisterViewModel
            {
                Email = "correo",
                Password = "123",
                ConfirmPassword = "123"
            };

            _controller.ModelState.AddModelError("Email", "Correo inválido");

            // Act
            var result = await _controller.Register(registerModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
        }

        // ✅ PRUEBA: Logout debería cerrar sesión y redirigir a Login
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
            return new Mock<SignInManager<Usuario>>(
                MockUserManager().Object,
                new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>().Object,
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
