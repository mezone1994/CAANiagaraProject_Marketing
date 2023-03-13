using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Category : Auditable
    {
        public int Id { get; set; }

        //[Display(Name = "Name")]
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = " cannot be more than 50 characters long.")]
        public string Name { get; set; }

        public bool IsLowInventory { get; set; }
        public int LowCategoryThreshold { get; set; } = 10;

        public ICollection<Item> Items { get; set; } = new HashSet<Item>();
    }
}
