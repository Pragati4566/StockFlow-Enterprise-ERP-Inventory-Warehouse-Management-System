namespace Domain.Entities
{
    public class Warehouse : AuditableEntity
    {
      
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
    }
}