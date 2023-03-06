namespace CAAMarketing.Models
{
    public class ItemLocation
    {

        public int LocationID { get; set; }
        public Location Location { get; set; }

        public int ItemID { get; set; }
        public Item Item { get; set; }
    }
}
