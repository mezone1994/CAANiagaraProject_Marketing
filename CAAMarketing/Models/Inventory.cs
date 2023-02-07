using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Inventory : Auditable
    {
        public int Id { get; set; }

        [Display(Name = "Cost")]
        [Required(ErrorMessage = "You must enter a cost.")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "You must enter a quantity.")]
        public int Quantity { get; set; }

        public int ItemID { get; set; }
        //Making a collection of item objects in this class
        [Display(Name = "Items")]
        public Item Item { get; set; }


        [Display(Name = "Location")]
        public int LocationID { get; set; }
        public Location Location { get; set; }

        public bool IsLowInventory { get; set; }
        public int LowInventoryThreshold { get; set; } = 10;
    }
}
