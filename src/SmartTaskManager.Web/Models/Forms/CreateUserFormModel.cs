using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Web.Models.Forms;

public sealed class CreateUserFormModel
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string UserName { get; set; } = string.Empty;
}
