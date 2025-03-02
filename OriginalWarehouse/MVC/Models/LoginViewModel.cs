using System.ComponentModel.DataAnnotations;

namespace OriginalWarehouse.MVC.Models
{
    /// <summary>
    /// Modelo de vista para la autenticación de usuarios.
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// Correo electrónico del usuario.
        /// </summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo electrónico válido.")]
        public string Email { get; set; }

        /// <summary>
        /// Contraseña del usuario.
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Indica si el usuario desea que su sesión se recuerde.
        /// </summary>
        public bool RememberMe { get; set; }
    }
}
