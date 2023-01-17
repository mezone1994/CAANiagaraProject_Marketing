using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class User
    {
        public int ID { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        public int loginTime { get; set; }

        public int ItemID { get; set; }
        //Making a collection of play objects in this class
        [Display(Name = "Items")]
        public ICollection<Item> Items { get; set; } = new HashSet<Item>();

    }
}
