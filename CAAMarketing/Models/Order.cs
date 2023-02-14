using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CAAMarketing.Models
{
    public class Order : Auditable, IValidatableObject
    {
        //PROPERTY FIELDS
        public int ID { get; set; }

        //Cost Summary Property
        [Display(Name = "Cost")]
        //[DataType(DataType.Currency)]
        public string CostString 
        {
            get
            {
                //convert decimal cost to string for ordering to work
                string newCost = Cost.ToString("C");
                return newCost;
            }
        }

        [Required(ErrorMessage = "You Need An Order Quantity!")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "You Need A Order Date!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DateMade { get; set; }

        [Required(ErrorMessage = "You Need A Order Delivery Date!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DeliveryDate { get; set; }

        [Display(Name = "Cost")]
        [Required(ErrorMessage = "You must enter a cost.")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }


        //Calling the Item to connect its table into this class
        [Display(Name = "Type of Item")]
        [Required(ErrorMessage = "You Must Select A Item")]
        public int ItemID { get; set; }

        [Display(Name = "Item Name")]
        public Item Item { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            //Create a string array containing the one element-the field where our error message should show up.
            //then you pass this to the ValidaitonResult This is only so the mesasge displays beside the field
            //instead of just in the validaiton summary.
            //var field = new[] { "DOB" };

            if (DateMade.GetValueOrDefault() > DateTime.Today)
            {
                yield return new ValidationResult("Date Made cannot be in the future.", new[] { "DateMade" });
            }

        }
    }
}