using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class ItemThumbNail
    {
        public int ID { get; set; }

        [ScaffoldColumn(false)]
        public byte[] Content { get; set; }

        [StringLength(255)]
        public string MimeType { get; set; }

        public int ItemID { get; set; }
        public Item Item { get; set; }
    }
}
