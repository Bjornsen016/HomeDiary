namespace HomeMaintenanceService.Model
{
    public class NewHomeTask
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> NotesList { get; set; } = new();
    }
}
