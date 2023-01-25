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
        [Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = "cannot be more than 50 characters long.")]
        public string Description { get; set; }

        [Display(Name = "Date")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        //[Display(Name = "Location")]
        //[Required(ErrorMessage = "You cannot leave it blank.")]
        //[StringLength(50, ErrorMessage = "cannot be more than 50 characters long.")]
        //public string location { get; set; }

        //Calling the Location Class to connect its table into this class
        [Display(Name = "Location Area")]
        [Required(ErrorMessage = "You Must Select A Location")]
        public int LocationID { get; set; }

        [Display(Name = "Location Name")]
        public Location Location { get; set; }

    }
}
