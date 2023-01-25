using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Transfer : Auditable
    {
        public int Id { get; set; }

        [Display(Name = "Transfer Date")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [DataType(DataType.Date)]
        public DateTime TransferDate { get; set; }

        [Display(Name = "Current Location")]
        [Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = " cannot be more than 50 characters long.")]
        public string CurrentLocation { get; set; }

        [Display(Name = "New Location")]
        [Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = " cannot be more than 50 characters long.")]
        public string NewLocation { get; set; }


        [Display(Name = "Location")]
        public int LocationID { get; set; }
        public Location Location { get; set; }

        [Display(Name = "Inventory")]
        [Required(ErrorMessage = "You Must Select")]
        public int InventoryID { get; set; }

        [Display(Name = "Inventory")]
        public Inventory Inventory { get; set; }



    }
}
