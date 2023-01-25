using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class InventoryTransfer : Auditable
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }
        public Item Item { get; set; }

        [Required]
        public int FromLocationId { get; set; }
        public Location FromLocation { get; set; }

        [Required]
        public int ToLocationId { get; set; }
        public Location ToLocation { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime TransferDate { get; set; }
    }
}
