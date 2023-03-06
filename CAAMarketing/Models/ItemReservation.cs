using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class ItemReservation
    {
        public int Id { get; set; }

        [Display(Name = "Event")]
        public int EventId { get; set; }
        public Event Event { get; set; }

        [Display(Name = "Item")]
        public int ItemId { get; set; }
        public Item Item { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Reserved Date")]
        [DataType(DataType.Date)]
        public DateTime? ReservedDate { get; set; }

        [Display(Name = "Return Date")]
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        [Display(Name = "Logged Out Date")]
        [DataType(DataType.Date)]
        public DateTime? LoggedOutDate { get; set; }

        [Display(Name = "Log Back In Date")]
        [DataType(DataType.Date)]
        public DateTime? LogBackInDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<EventLog> EventLogs { get; set; }
    }
}
