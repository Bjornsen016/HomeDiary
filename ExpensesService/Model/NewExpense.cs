﻿using System.ComponentModel.DataAnnotations;

namespace ExpensesService.Model
{
    public class NewExpense
    {
        [Required]
        public string? Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public string? Category { get; set; }
        [Required]
        public DateOnly ExpenseDate { get; set; }
        public DateOnly WarrantyEndDate { get; set; }
        public string? ExpenseImageUri { get; set; }
        [Required]
        public string? UserId { get; set; }
        public string? TaskId { get; set; }
    }
}
