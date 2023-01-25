using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Location : Auditable
    {
        public int Id { get; set; }

        [Display(Name = "Location Name")]
        [Required(ErrorMessage = "You cannot leave the name blank.")]
        [StringLength(150, ErrorMessage = "name cannot be more than 150 characters long.")]
        public string Name { get; set; }

        public ICollection<InventoryTransfer> InventoryTransfersFrom { get; set; }
        public ICollection<InventoryTransfer> InventoryTransfersTo { get; set; }

    }
}
