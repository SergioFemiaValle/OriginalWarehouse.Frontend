using System.ComponentModel.DataAnnotations;

namespace OriginalWarehouse.MVC.Models
{
    /// <summary>
    /// Modelo de vista para el registro de nuevos usuarios.
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary>
        /// Nombre del usuario.
        /// </summary>
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; }

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
        /// Confirmación de la contraseña ingresada por el usuario.
        /// Debe coincidir con la contraseña original.
        /// </summary>
        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }
    }
}
