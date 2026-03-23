namespace oop_s2_2_mvc_77487.Models
{
    public class Inspection
    {
        public int Id { get; set; }
        public int PremisesId { get; set; }
        public DateTime InspectionDate { get; set; }
        public int Score { get; set; } // 0-100
        public InspectionOutcome Outcome { get; set; }
        public string? Notes { get; set; }

        public Premises? Premises { get; set; }
        public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    }

    public enum InspectionOutcome
    {
        Pass,
        Fail
    }
}
