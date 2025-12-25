namespace FlexiBase.Models
{
    /// <summary>
    /// Configuration pour un champ de type Select
    /// </summary>
    public class SelectFieldConfig
    {
        public List<SelectOption> Options { get; set; } = new();
        public bool AllowCustom { get; set; } = false;
        public string? DefaultValue { get; set; }
    }
}
