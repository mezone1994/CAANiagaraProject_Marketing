using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Event : Auditable
    {
        public int ID { get; set; }

        [Display(Name = "Event Name")]
        [Required(ErrorMessage = "You cannot leave the Event name blank.")]
        [StringLength(150, ErrorMessage = "name cannot be more than 150 characters long.")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Location")]
        [Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = "cannot be more than 50 characters long.")]
        public string location { get; set; }


        [Required]
        [Display(Name = "Reserved Date")]
        [DataType(DataType.Date)]
        public DateTime? ReservedEventDate { get; set; }

        [Required]
        [Display(Name = "Return Date")]
        [DataType(DataType.Date)]
        public DateTime? ReturnEventDate { get; set; }

        public ICollection<ItemReservation> ItemReservations { get; set; }

    }
}
