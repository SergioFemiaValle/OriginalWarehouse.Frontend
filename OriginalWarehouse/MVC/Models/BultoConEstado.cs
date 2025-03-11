using OriginalWarehouse.Domain.Entities;

namespace OriginalWarehouse.Web.MVC.Models
{
    public class BultoConEstado
    {
        public Bulto Bulto { get; set; }
        public bool TieneEntrada { get; set; }
        public bool TieneSalida { get; set; }
    }
}
