﻿using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Subscription : Auditable
    {
        public int Id { get; set; }

        [StringLength(512)]
        public string PushEndpoint { get; set; }

        [StringLength(512)]
        public string PushP256DH { get; set; }

        [StringLength(512)]
        public string PushAuth { get; set; }

        [Required(ErrorMessage = "You must select the Staff Member.")]
        [Display(Name = "Employee")]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }
    }
}
