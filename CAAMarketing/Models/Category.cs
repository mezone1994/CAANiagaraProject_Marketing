using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Category : Auditable
    {
        public int Id { get; set; }

        //[Display(Name = "Name")]
        //[Required(ErrorMessage = "You cannot leave it blank.")]
        //[StringLength(50, ErrorMessage = " cannot be more than 50 characters long.")]
        public string Name { get; set; }

        public ICollection<Item> Items { get; set; } = new HashSet<Item>();
    }
}
