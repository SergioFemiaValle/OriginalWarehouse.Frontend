using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Models
{
    public class DetalleBultoConEstado
    {
        public DetalleBulto Detalle { get; set; }
        public bool TieneEntrada { get; set; }
        public bool TieneSalida { get; set; }
    }
}
