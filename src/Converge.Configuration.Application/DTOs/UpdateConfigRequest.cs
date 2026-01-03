namespace Converge.Configuration.DTOs
{
    public class UpdateConfigRequest
    {
        public string Value { get; set; } = null!;
        public int? ExpectedVersion { get; set; }
    }
}
