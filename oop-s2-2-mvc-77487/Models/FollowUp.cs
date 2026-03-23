namespace oop_s2_2_mvc_77487.Models
{
    public class FollowUp
    {
        public int Id { get; set; }
        public int InspectionId { get; set; }
        public DateTime DueDate { get; set; }
        public FollowUpStatus Status { get; set; } = FollowUpStatus.Open;
        public DateTime? ClosedDate { get; set; }

        public Inspection? Inspection { get; set; }
    }

    public enum FollowUpStatus
    {
        Open,
        Closed
    }
}
