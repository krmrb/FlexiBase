namespace FlexiBase.Models.interfaces
{
    /// <summary>
    /// Interface pour les entités avec soft delete
    /// </summary>
    public interface ISoftDeletable
    {
        DateTime? DeletedAt { get; }
        bool IsDeleted { get; }
    }
}
