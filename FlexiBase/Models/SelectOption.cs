namespace FlexiBase.Models
{
    public class SelectOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int Order { get; set; } = 1;
    }
}
