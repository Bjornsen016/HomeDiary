using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8618

namespace HomeMaintenanceService.Model
{
    public class HomeTask
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Category { get; set; }
        [Required]
        public string Description { get; set; }
        public List<Note>? NotesList { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        [Required]
        public string UserId { get; set; }
    }

    public class Note
    {
        public Guid Id { get; set; }
        public string Text { get; set; }

    }
}

