using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class EventLog
    {
        public int Id { get; set; }

        [Display(Name = "Event Name")]
        public string EventName { get; set; }

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        public int Quantity { get; set; }

        [Display(Name = "Log Date")]
        [DataType(DataType.Date)]
        public DateTime LogDate { get; set; }

        public int? ItemReservationId { get; set; } // Nullable foreign key

        public ItemReservation ItemReservation { get; set; }
    }
}
