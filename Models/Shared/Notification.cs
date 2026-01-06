namespace MessManagementSystem.Models.Shared  // ✅ Must match
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ These are the new columns
        public int? BillIssueId { get; set; }
        public int? ContactMessageId { get; set; }
        public int? BillId { get; set; }
    }
}