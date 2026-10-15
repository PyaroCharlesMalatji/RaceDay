namespace RaceDayApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";          // e.g. Under 20, Senior, 10km, 21km
        public int EventId { get; set; }
        public Event? Event { get; set; }
    }
}
