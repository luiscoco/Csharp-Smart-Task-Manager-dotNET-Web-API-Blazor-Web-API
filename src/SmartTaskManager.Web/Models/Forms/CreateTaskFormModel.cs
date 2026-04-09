using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Web.Models.Forms;

public sealed class CreateTaskFormModel
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateOnly? DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(2));

    [Required]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [StringLength(100)]
    public string? CategoryName { get; set; }

    [Required]
    public TaskKind TaskType { get; set; } = TaskKind.Work;
}
