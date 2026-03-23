namespace oop_s2_2_mvc_77487.Utilities;

using oop_s2_2_mvc_77487.Models;

public static class ValidationExtensions
{
    public static bool IsValidScore(int score)
    {
        return score >= 0 && score <= 100;
    }

    public static InspectionOutcome GetOutcomeFromScore(int score)
    {
        return score >= 75 ? InspectionOutcome.Pass : InspectionOutcome.Fail;
    }

    public static bool IsValidFollowUpDate(DateTime inspectionDate, DateTime dueDate)
    {
        return dueDate >= inspectionDate;
    }

    public static bool IsOverdue(DateTime dueDate, FollowUpStatus status)
    {
        return DateTime.Now > dueDate && status == FollowUpStatus.Open;
    }
}
