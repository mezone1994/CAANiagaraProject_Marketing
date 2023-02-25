using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class Item : Auditable, IValidatableObject
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

        [Display(Name = "UPC")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [StringLength(50, ErrorMessage = "cannot be more than 50 characters long.")]
        public string UPC { get; set; }

        [Display(Name = "Date Received")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DateReceived { get; set; }

        public ItemImages ItemImages { get; set; }

        public ItemThumbNail ItemThumbNail { get; set; }


        //Calling the Supplier to connect its table into this class
        [Display(Name = "Type of Supplier")]
        [Required(ErrorMessage = "You Must Select A Supplier Name")]
        public int SupplierID { get; set; }

        [Display(Name = "Supplier Name")]
        public Supplier Supplier { get; set; }

        [Display(Name = "Category")]
        [Required(ErrorMessage = "You cannot leave it blank.")]

        public int CategoryID { get; set; }
        public Category Category { get; set; }

        public ICollection<InventoryTransfer> InventoryTransfers { get; set; }

        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }

        public bool Archived { get; set; } = false;

        public ICollection<Order> Orders { get; set; }

        [Display(Name = "Cost")]
        [Required(ErrorMessage = "You must enter a cost.")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "You must enter a quantity.")]
        public int Quantity { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            //Create a string array containing the one element-the field where our error message should show up.
            //then you pass this to the ValidaitonResult This is only so the mesasge displays beside the field
            //instead of just in the validaiton summary.
            //var field = new[] { "DOB" };

            if (DateReceived.GetValueOrDefault() > DateTime.Today)
            {
                yield return new ValidationResult("Date Received cannot be in the future.", new[] { "DateReceived" });
            }

        }
    }
}
