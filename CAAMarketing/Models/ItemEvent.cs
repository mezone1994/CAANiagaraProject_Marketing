using System.Diagnostics.Metrics;

namespace CAAMarketing.Models
{
    public class ItemEvent
    {

        public int EventID { get; set; }
        public Event Event { get; set; }

        public int ItemID { get; set; }
        public Item Item { get; set; }
    }
}
