using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Archive : Auditable
    {
        public int Id { get; set; }

        public int ItemID { get; set; }

        //Making a collection of play objects in this class
        [Display(Name = "Items")]
        public Item Item { get; set; }

    }
}
