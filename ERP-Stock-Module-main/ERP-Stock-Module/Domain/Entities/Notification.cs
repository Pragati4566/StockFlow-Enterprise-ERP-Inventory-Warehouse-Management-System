namespace Domain.Entities
{
    public class Notification : AuditableEntity
    {
    
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public string? UserId { get; set; } // اختياري: لتوجيه الإشعار لمستخدم محدد
    }
}