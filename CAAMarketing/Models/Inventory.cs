using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Inventory
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
        //Making a collection of play objects in this class
        [Display(Name = "Items")]
        public ICollection<Item> Items { get; set; } = new HashSet<Item>();

        [Display(Name = "User Name")]
        public int userID { get; set; }
        public User users { get; set; }

        [Display(Name = "Location")]
        public int LocationID { get; set; }
        public Location Location { get; set; }
    }
}
