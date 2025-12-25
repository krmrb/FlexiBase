namespace FlexiBase.Models
{
    public class SelectFieldConfiguration
    {
        public List<SelectOption> Options { get; set; } = new();
        public bool AllowCustom { get; set; } = false;
        public string? DefaultValue { get; set; }
    }
}
