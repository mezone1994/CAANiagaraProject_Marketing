using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CAAMarketing.Models
{
    public class Equipment : Auditable
    {
        //PROPERTY FIELDS
        public int ID { get; set; }

        [Required(ErrorMessage = "You Need A Equipment Name!")]
        [StringLength(30, ErrorMessage = "Equipment name cannot be more than 30 characters long! Please Try Again...")]
        public string Name { get; set; }

        [Required(ErrorMessage = "You Need A Equipment Description!")]
        [StringLength(50, ErrorMessage = "Description cannot be more than 50 characters long! Please Try Again...")]
        public string Description { get; set; }

        

    }
}