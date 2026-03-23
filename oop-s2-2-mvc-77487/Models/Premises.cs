namespace oop_s2_2_mvc_77487.Models
{
    public class Premises
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Town { get; set; } = string.Empty;
        public RiskRating RiskRating { get; set; } = RiskRating.Medium;

        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    }

    public enum RiskRating
    {
        Low,
        Medium,
        High
    }
}
