using System.ComponentModel.DataAnnotations;

namespace IntelR.Shared;

[Serializable]
public abstract class GenerateRequestBase
{
    [Required]
    public string ChatKey { get; set; } = string.Empty;
}