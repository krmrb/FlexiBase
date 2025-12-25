using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FlexiBase.Models.interfaces;
using FlexiBase.Models.enums;
using FlexiBase.Models;

namespace ERP.Core.Models.ContentItems
{
    /// <summary>
    /// Représente une instance concrète d'un Content Type
    /// Exemple: Un produit spécifique "iPhone 15" est un Content Item du Content Type "Produit"
    /// </summary>
    [Table("ContentItems")]
    public class ContentItem : IAuditable, ISoftDeletable
    {
        #region Propriétés d'Identité

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Identifiant public unique
        /// </summary>
        [Required]
        [MaxLength(36)]
        public string Uuid { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Nom ou titre de l'item
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; private set; } = string.Empty;

        /// <summary>
        /// Slug pour les URLs
        /// </summary>
        [MaxLength(200)]
        public string? Slug { get; private set; }

        #endregion

        #region Propriétés d'État

        [Required]
        public ContentItemStatus Status { get; private set; } = ContentItemStatus.Draft;

        [Required]
        public int Version { get; private set; } = 1;

        [Required]
        public bool IsCurrentVersion { get; private set; } = true;

        public int? PreviousVersionId { get; private set; }

        #endregion

        #region Propriétés de Relation

        [Required]
        public int CustomEntityId { get; private set; }

        [ForeignKey(nameof(CustomEntityId))]
        public virtual CustomEntity CustomEntity { get; private set; } = null!;

        public int? ParentItemId { get; private set; }

        [ForeignKey(nameof(ParentItemId))]
        public virtual ContentItem? ParentItem { get; private set; }

        public virtual ICollection<ContentItem> Children { get; private set; } = new List<ContentItem>();

        // CORRECTION: Maintenant FieldValue est défini avant, donc pas d'erreur
        public virtual ICollection<FieldValue> FieldValues { get; private set; } = new List<FieldValue>();

        public virtual ICollection<ContentItemHistory> History { get; private set; } = new List<ContentItemHistory>();

        public virtual ICollection<ContentItemRelation> Relations { get; private set; } = new List<ContentItemRelation>();

        #endregion

        #region Propriétés d'Audit

        [Required]
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; private set; } = string.Empty;

        public DateTime? UpdatedAt { get; private set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; private set; }

        public DateTime? PublishedAt { get; private set; }

        [MaxLength(100)]
        public string? PublishedBy { get; private set; }

        public DateTime? ArchivedAt { get; private set; }

        public DateTime? DeletedAt { get; private set; }

        public bool IsDeleted => DeletedAt.HasValue;

        [Timestamp]
        public byte[]? RowVersion { get; private set; }

        #endregion

        #region Constructeurs

        protected ContentItem() { }

        public ContentItem(
            CustomEntity customEntity,
            string displayName,
            string createdBy,
            ContentItem? parentItem = null)
        {
            ValidateCreation(customEntity, displayName, createdBy);

            CustomEntity = customEntity;
            CustomEntityId = customEntity.Id;
            DisplayName = displayName.Trim();
            CreatedBy = createdBy;

            GenerateSlug();

            if (parentItem != null)
            {
                ParentItem = parentItem;
                ParentItemId = parentItem.Id;
            }

            AddHistory("Création de l'item", createdBy);
        }

        #endregion

        #region Méthodes de Gestion du Cycle de Vie

        public void Publish(string publishedBy)
        {
            if (Status == ContentItemStatus.Published)
                throw new InvalidOperationException("L'item est déjà publié");

            if (Status == ContentItemStatus.Archived)
                throw new InvalidOperationException("Impossible de publier un item archivé");

            ValidateRequiredFields();

            Status = ContentItemStatus.Published;
            PublishedAt = DateTime.UtcNow;
            PublishedBy = publishedBy;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = publishedBy;

            AddHistory("Publication de l'item", publishedBy);
        }

        public void Unpublish(string updatedBy)
        {
            if (Status != ContentItemStatus.Published)
                throw new InvalidOperationException("Seuls les items publiés peuvent être dépubliés");

            Status = ContentItemStatus.Draft;
            PublishedAt = null;
            PublishedBy = null;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory("Dépublication de l'item", updatedBy);
        }

        public void Archive(string archivedBy)
        {
            if (Status == ContentItemStatus.Archived)
                throw new InvalidOperationException("L'item est déjà archivé");

            Status = ContentItemStatus.Archived;
            ArchivedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = archivedBy;

            AddHistory("Archivage de l'item", archivedBy);
        }

        public void Unarchive(string updatedBy)
        {
            if (Status != ContentItemStatus.Archived)
                throw new InvalidOperationException("Seuls les items archivés peuvent être désarchivés");

            Status = ContentItemStatus.Draft;
            ArchivedAt = null;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory("Désarchivage de l'item", updatedBy);
        }

        public ContentItem CreateNewVersion(string createdBy)
        {
            IsCurrentVersion = false;

            var newVersion = new ContentItem(
                CustomEntity,
                DisplayName,
                createdBy,
                ParentItem);

            newVersion.Version = Version + 1;
            newVersion.PreviousVersionId = Id;
            newVersion.Status = ContentItemStatus.Draft;

            foreach (var fieldValue in FieldValues)
            {
                newVersion.SetFieldValue(
                    fieldValue.ContentField.SystemName,
                    fieldValue.GetValue(),
                    createdBy);
            }

            AddHistory($"Création de la version {newVersion.Version}", createdBy);

            return newVersion;
        }

        public void RestoreVersion(ContentItem versionToRestore, string restoredBy)
        {
            if (versionToRestore.Id == Id)
                throw new InvalidOperationException("Impossible de restaurer la version courante");

            if (versionToRestore.CustomEntityId != CustomEntityId)
                throw new InvalidOperationException("Les Content Types ne correspondent pas");

            foreach (var fieldValue in versionToRestore.FieldValues)
            {
                SetFieldValue(
                    fieldValue.ContentField.SystemName,
                    fieldValue.GetValue(),
                    restoredBy);
            }

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = restoredBy;

            AddHistory($"Restauration depuis la version {versionToRestore.Version}", restoredBy);
        }

        public void Delete(string deletedBy)
        {
            if (Status == ContentItemStatus.Published)
                throw new InvalidOperationException("Impossible de supprimer un item publié. Dépublié-le d'abord.");

            Status = ContentItemStatus.Deleted;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = deletedBy;

            AddHistory("Suppression de l'item", deletedBy);
        }

        public void Restore(string restoredBy)
        {
            if (!IsDeleted)
                throw new InvalidOperationException("L'item n'est pas supprimé");

            Status = ContentItemStatus.Draft;
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = restoredBy;

            AddHistory("Restauration de l'item", restoredBy);
        }

        #endregion

        #region Méthodes de Gestion des Valeurs de Champs

        public void SetFieldValue(string fieldSystemName, object? value, string updatedBy)
        {
            var field = CustomEntity.Fields.FirstOrDefault(f => f.SystemName == fieldSystemName);

            if (field == null)
                throw new ArgumentException($"Champ '{fieldSystemName}' introuvable", nameof(fieldSystemName));

            if (!field.ValidateValue(value, out var errors))
            {
                throw new ValidationException($"Validation échouée pour '{field.Name}': {string.Join(", ", errors)}");
            }

            var existingValue = FieldValues.FirstOrDefault(fv => fv.ContentField.SystemName == fieldSystemName);

            if (existingValue == null)
            {
                var fieldValue = new FieldValue(this, field, value);
                FieldValues.Add(fieldValue);
            }
            else
            {
                existingValue.SetValue(value);
            }

            if (IsTitleField(fieldSystemName) && value != null)
            {
                DisplayName = value.ToString()!;
                GenerateSlug();
            }

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory($"Mise à jour du champ '{field.Name}'", updatedBy);
        }

        public object? GetFieldValue(string fieldSystemName)
        {
            var fieldValue = FieldValues.FirstOrDefault(fv => fv.ContentField.SystemName == fieldSystemName);

            if (fieldValue == null)
            {
                var field = CustomEntity.Fields.FirstOrDefault(f => f.SystemName == fieldSystemName);
                return field?.DefaultValue != null ? field.ConvertFromStorage(field.DefaultValue) : null;
            }

            return fieldValue.GetValue();
        }

        public T? GetFieldValue<T>(string fieldSystemName)
        {
            var value = GetFieldValue(fieldSystemName);

            if (value == null)
                return default;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public Dictionary<string, object?> GetAllFieldValues()
        {
            var result = new Dictionary<string, object?>();

            foreach (var field in CustomEntity.Fields)
            {
                result[field.SystemName] = GetFieldValue(field.SystemName);
            }

            return result;
        }

        private void ValidateRequiredFields()
        {
            var errors = new List<string>();

            foreach (var field in CustomEntity.Fields.Where(f => f.IsRequired))
            {
                var value = GetFieldValue(field.SystemName);

                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    errors.Add($"Le champ '{field.Name}' est obligatoire");
                }
            }

            if (errors.Count > 0)
            {
                throw new ValidationException($"Validation échouée: {string.Join(", ", errors)}");
            }
        }

        private bool IsTitleField(string fieldSystemName)
        {
            var titleFields = new[] { "title", "name", "nom", "titre", "libelle" };
            return titleFields.Contains(fieldSystemName.ToLowerInvariant());
        }

        private void GenerateSlug()
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
                return;

            var slug = DisplayName.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("\"", "");

            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            Slug = slug;
        }

        #endregion

        #region Méthodes de Gestion des Relations

        public void AddRelation(
            ContentItem relatedItem,
            string relationType,
            string? metadata = null,
            string updatedBy = "")
        {
            if (relatedItem == null)
                throw new ArgumentNullException(nameof(relatedItem));

            var existingRelation = Relations.FirstOrDefault(r =>
                r.RelatedItemId == relatedItem.Id && r.RelationType == relationType);

            if (existingRelation == null)
            {
                var relation = new ContentItemRelation(
                    this,
                    relatedItem,
                    relationType,
                    metadata);

                Relations.Add(relation);
                UpdatedAt = DateTime.UtcNow;
                UpdatedBy = updatedBy;

                AddHistory($"Ajout de la relation '{relationType}' avec '{relatedItem.DisplayName}'", updatedBy);
            }
        }

        public void RemoveRelation(ContentItem relatedItem, string relationType, string updatedBy)
        {
            var relation = Relations.FirstOrDefault(r =>
                r.RelatedItemId == relatedItem.Id && r.RelationType == relationType);

            if (relation != null)
            {
                Relations.Remove(relation);
                UpdatedAt = DateTime.UtcNow;
                UpdatedBy = updatedBy;

                AddHistory($"Suppression de la relation '{relationType}' avec '{relatedItem.DisplayName}'", updatedBy);
            }
        }

        public List<ContentItem> GetRelatedItems(string relationType)
        {
            return Relations
                .Where(r => r.RelationType == relationType && r.RelatedItem != null)
                .Select(r => r.RelatedItem!)
                .ToList();
        }

        #endregion

        #region Méthodes de Recherche

        public bool MatchesSearch(string searchText, bool searchInFields = true)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var searchLower = searchText.ToLowerInvariant();

            if (DisplayName.ToLowerInvariant().Contains(searchLower))
                return true;

            if (searchInFields)
            {
                foreach (var fieldValue in FieldValues)
                {
                    var value = fieldValue.GetValue()?.ToString();
                    if (!string.IsNullOrWhiteSpace(value) &&
                        value.ToLowerInvariant().Contains(searchLower))
                        return true;
                }
            }

            return false;
        }

        public bool MatchesFilters(Dictionary<string, object> filters)
        {
            foreach (var filter in filters)
            {
                var fieldValue = GetFieldValue(filter.Key);

                if (fieldValue == null && filter.Value != null)
                    return false;

                if (fieldValue != null && !fieldValue.Equals(filter.Value))
                    return false;
            }

            return true;
        }

        #endregion

        #region Méthodes Helper Privées

        private void ValidateCreation(
            CustomEntity customEntity,
            string displayName,
            string createdBy)
        {
            if (customEntity == null)
                throw new ArgumentNullException(nameof(customEntity));

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Le nom d'affichage est requis", nameof(displayName));

            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("L'utilisateur créateur est requis", nameof(createdBy));

            if (customEntity.Status != CustomEntityStatus.Active)
                throw new InvalidOperationException($"Le Content Type '{customEntity.Name}' n'est pas actif");
        }

        private void AddHistory(string description, string changedBy)
        {
            History.Add(new ContentItemHistory
            {
                ContentItem = this,
                Version = Version,
                Description = description,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy
            });
        }

        #endregion

        #region Méthodes d'Utilitaire

        public bool CanModify()
        {
            return Status == ContentItemStatus.Draft && !IsDeleted;
        }

        public bool IsPublished()
        {
            return Status == ContentItemStatus.Published;
        }

        public ContentItemSummary GetSummary()
        {
            return new ContentItemSummary
            {
                Id = Id,
                Uuid = Uuid,
                DisplayName = DisplayName,
                CustomEntityName = CustomEntity.Name,
                Status = Status,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }

        public override string ToString() => $"{DisplayName} ({CustomEntity.Name})";

        #endregion
    }
}