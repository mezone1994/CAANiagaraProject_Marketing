﻿using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public abstract class Auditable : IAuditable
    {
        [ScaffoldColumn(false)]
        [StringLength(256)]
        public string CreatedBy { get; set; }

        [ScaffoldColumn(false)]
        public DateTime? CreatedOn { get; set; }

        [ScaffoldColumn(false)]
        [StringLength(256)]
        public string UpdatedBy { get; set; }

        [ScaffoldColumn(false)]
        public DateTime? UpdatedOn { get; set; }

        [ScaffoldColumn(false)]
        [Timestamp]
        public Byte[] RowVersion { get; set; }//Added for concurrency

        [ScaffoldColumn(false)]
        [StringLength(256)]
        public string EmployeeNameUser { get; set; }
    }
}
