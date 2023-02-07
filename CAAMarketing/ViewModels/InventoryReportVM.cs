using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.ViewModels
{
    public class InventoryReportVM
    {
        public int ID { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; }

        [Display(Name = "UPC")]
        public string UPC { get; set; }

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        [Display(Name = "Cost")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Display(Name = "Quantity")]
        public int quantity { get; set; }

        [Display(Name = "Location")]
        public string location { get; set; }

        [Display(Name = "Type of Supplier")]
        public string supplier { get; set; }

        [Display(Name = "Date Received")]
        [DataType(DataType.Date)]
        public DateTime DateReceived { get; set; }

        public string Notes { get; set; }


    }
}
