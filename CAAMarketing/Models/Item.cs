using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class Item
    {
        public int ID { get; set; }

        [Display(Name = "Item Name")]
        [Required(ErrorMessage = "You cannot leave the item name blank.")]
        [StringLength(150, ErrorMessage = "name cannot be more than 150 characters long.")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = " cannot be more than 50 characters long.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "You cannot leave the notes blank.")]
        [MinLength(10, ErrorMessage = "notes must be at least 10 characters.")]
        [MaxLength(4000, ErrorMessage = "notes cannot be more than 4,000 characters.")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Category")]
        [Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = " cannot be more than 50 characters long.")]
        public string Category { get; set; }

        [Display(Name = "UPC")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [StringLength(50, ErrorMessage = "cannot be more than 50 characters long.")]
        public string UPC { get; set; }

        [Display(Name = "Date Received")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [DataType(DataType.Date)]
        public DateTime DateReceived { get; set; }

        public ItemImages ItemImages { get; set; }

        //Calling the Supplier to connect its table into this class
        [Display(Name = "Type of Supplier")]
        [Required(ErrorMessage = "You Must Select A Supplier Name")]
        public int SupplierID { get; set; }

        [Display(Name = "Supplier Name")]
        public Supplier Supplier { get; set; }


    }
}
