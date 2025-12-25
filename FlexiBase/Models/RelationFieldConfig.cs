namespace FlexiBase.Models
{
    /// <summary>
    /// Configuration pour un champ de type Relation
    /// </summary>
    public class RelationFieldConfig
    {
        public int? RequiredRelationId { get; set; }
        public string? DisplayField { get; set; } = "Name";
        public bool AllowMultiple { get; set; } = false;
        public string? Filter { get; set; }
        public string? OrderBy { get; set; } = "Name";
        public bool IsCascading { get; set; } = false;
    }
}
