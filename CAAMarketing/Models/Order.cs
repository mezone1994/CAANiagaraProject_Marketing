using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CAAMarketing.Models
{
    public class Order : Auditable
    {
        //PROPERTY FIELDS
        public int ID { get; set; }

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

        //Calling the Item to connect its table into this class
        [Display(Name = "Type of Item")]
        [Required(ErrorMessage = "You Must Select A Item")]
        public int ItemID { get; set; }

        [Display(Name = "Item Name")]
        public Item Item { get; set; }

    }
}