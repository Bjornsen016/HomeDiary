namespace HomeMaintenanceService.Model
{
    public class HomeTaskDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> NotesList { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }
}
