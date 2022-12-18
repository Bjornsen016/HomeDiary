using System.ComponentModel.DataAnnotations;

namespace ExpensesService.Model
{
    public class Expense
    {
        [Key]
        public Guid ExpenseId { get; set; }
        [Required]
        public string? Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public string? Category { get; set; }
        [Required]
        public DateTime ExpenseDate { get; set; }
        public DateTime WarrantyEndDate { get; set; }
        public string? ExpenseImageUri { get; set; }

        public string? UserId { get; set; }
        public string? TaskId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedAt { get; set; }
    }
}
