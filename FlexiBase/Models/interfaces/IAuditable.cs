namespace FlexiBase.Models.interfaces
{
    /// <summary>
    /// Interface pour les entités auditées
    /// </summary>
    public interface IAuditable
    {
        DateTime CreatedAt { get; }
        string? CreatedBy { get; }
        DateTime? UpdatedAt { get; }
        string? UpdatedBy { get; }
    }
}
