namespace CAAMarketing.ViewModels
{
    public class LogBackInViewModel
    {
        public int ItemReservationId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public DateTime ReservedDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public DateTime LogBackInDate { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
    }
}
