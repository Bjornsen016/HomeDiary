using System.ComponentModel.DataAnnotations;

namespace ExpensesService.Model
{
    public class Expanse
    {
        public Guid Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public DateOnly ExpanseDate { get; set; }
        public DateOnly WarrantyEndDate { get; set; }
        public string? ExpanseImageUri { get; set; }
    }
}
