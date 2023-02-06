using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class ItemImages
    {
        public int Id { get; set; }

        [ScaffoldColumn(false)]
        public byte[] Content { get; set; }

        [StringLength(255)]
        public string MimeType { get; set; }

        public int InventoryID { get; set; }
        public Inventory Inventory { get; set; }
    }
}
