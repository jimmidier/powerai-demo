using System.ComponentModel.DataAnnotations;

namespace IntelR.Shared;

[Serializable]
public class GenerateReplyRequest : GenerateRequestBase
{
    [Required]
    public string TopicName { get; set; } = string.Empty;

    public bool UseUserIntent { get; set; }

    public string? UserIntent { get; set; }
}