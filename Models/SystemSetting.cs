using System.ComponentModel.DataAnnotations;

namespace HomeMaids.Models;

/// <summary>Simple key-value system flags (maintenance mode, feature toggles, etc.).</summary>
public class SystemSetting
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Key { get; set; } = string.Empty;
    [StringLength(2000)] public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
