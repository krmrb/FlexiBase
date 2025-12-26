using FlexiBase.Models;
using FlexiBase.Models.enums;
using FlexiBase.Models.interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Représente une instance concrète d'un Custom Type
    /// Exemple: Un produit spécifique "iPhone 15" est un Custom Item du Custom Type "Produit"
    /// </summary>
    [Table("CustomItems")]
    public class CustomItem : IAuditable, ISoftDeletable
    {
        #region Propriétés d'Identité

        /// <summary>
        /// Clé primaire auto-incrémentée par la base.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Identifiant public unique obligatoire (36 chars = format GUID).
        /// </summary>
        [Required]
        [MaxLength(36)]
        public string Uuid { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Nom d'affichage obligatoire (max 200 caractères).
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; private set; } = string.Empty;

        /// <summary>
        /// Identifiant URL optionnel (max 200 caractères). Le but d’améliorer la visibilité et le classement dans les résultats des moteurs de recherche.
        /// </summary>
        [MaxLength(200)]
        public string? Slug { get; private set; }

        #endregion

        #region Propriétés d'État

        /// <summary>
        /// Statut obligatoire (Draft, Published, Archived, Deleted).
        /// </summary>
        [Required]
        public CustomItemStatus Status { get; private set; } = CustomItemStatus.Draft;

        /// <summary>
        /// Numéro de version obligatoire (commence à 1).
        /// </summary>
        [Required]
        public int Version { get; private set; } = 1;

        /// <summary>
        /// Indicateur de version active obligatoire.
        /// </summary>
        [Required]
        public bool IsCurrentVersion { get; private set; } = true;

        /// <summary>
        /// Référence optionnelle à la version précédente.
        /// </summary>
        public int? PreviousVersionId { get; private set; }

        #endregion

        #region Propriétés de Relation

        /// <summary>
        /// Clé étrangère obligatoire vers le CustomEntity parent.
        /// </summary>
        [Required]
        public int CustomEntityId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        [ForeignKey(nameof(CustomEntityId))]
        public virtual CustomEntity CustomEntity { get; private set; } = null!;

        /// <summary>
        /// Clé étrangère optionnelle vers l'item parent (hiérarchie).
        /// </summary>
        public int? ParentItemId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        [ForeignKey(nameof(ParentItemId))]
        public virtual CustomItem? ParentItem { get; private set; }

        /// <summary>
        /// Collection d'items enfants (virtual = lazy loading).
        /// </summary>
        public virtual ICollection<CustomItem> Children { get; private set; } = new List<CustomItem>();

        /// <summary>
        /// Collection des valeurs des champs.
        /// </summary>
        public virtual ICollection<FieldValue> FieldValues { get; private set; } = new List<FieldValue>();

        /// <summary>
        /// Collection des entrées d'historique.
        /// </summary>
        public virtual ICollection<CustomItemHistory> History { get; private set; } = new List<CustomItemHistory>();

        /// <summary>
        /// Collection des relations avec d'autres items.
        /// </summary>
        public virtual ICollection<CustomItemRelation> Relations { get; private set; } = new List<CustomItemRelation>();

        #endregion

        #region Propriétés d'Audit

        /// <summary>
        /// 
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// 
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; private set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public DateTime? UpdatedAt { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(100)]
        public string? UpdatedBy { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? PublishedAt { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(100)]
        public string? PublishedBy { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? ArchivedAt { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? DeletedAt { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;

        /// <summary>
        /// 
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; private set; }

        #endregion

        #region Constructeurs

        protected CustomItem() { }

        public CustomItem(
            CustomEntity customEntity,
            string displayName,
            string createdBy,
            CustomItem? parentItem = null)
        {
            ValidateCreation(customEntity, displayName, createdBy);

            CustomEntity = customEntity;            // ERROR CS1717
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
            if (Status == CustomItemStatus.Published)
                throw new InvalidOperationException("L'item est déjà publié");

            if (Status == CustomItemStatus.Archived)
                throw new InvalidOperationException("Impossible de publier un item archivé");

            ValidateRequiredFields();

            Status = CustomItemStatus.Published;
            PublishedAt = DateTime.UtcNow;
            PublishedBy = publishedBy;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = publishedBy;

            AddHistory("Publication de l'item", publishedBy);
        }

        public void Unpublish(string updatedBy)
        {
            if (Status != CustomItemStatus.Published)
                throw new InvalidOperationException("Seuls les items publiés peuvent être dépubliés");

            Status = CustomItemStatus.Draft;
            PublishedAt = null;
            PublishedBy = null;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory("Dépublication de l'item", updatedBy);
        }

        public void Archive(string archivedBy)
        {
            if (Status == CustomItemStatus.Archived)
                throw new InvalidOperationException("L'item est déjà archivé");

            Status = CustomItemStatus.Archived;
            ArchivedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = archivedBy;

            AddHistory("Archivage de l'item", archivedBy);
        }

        public void Unarchive(string updatedBy)
        {
            if (Status != CustomItemStatus.Archived)
                throw new InvalidOperationException("Seuls les items archivés peuvent être désarchivés");

            Status = CustomItemStatus.Draft;
            ArchivedAt = null;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory("Désarchivage de l'item", updatedBy);
        }

        public CustomItem CreateNewVersion(string createdBy)
        {
            IsCurrentVersion = false;

            var newVersion = new CustomItem(
                CustomEntity,
                DisplayName,
                createdBy,
                ParentItem);

            newVersion.Version = Version + 1;
            newVersion.PreviousVersionId = Id;
            newVersion.Status = CustomItemStatus.Draft;

            foreach (var fieldValue in FieldValues)
            {
                newVersion.SetFieldValue(
                    fieldValue.CustomField.SystemName,
                    fieldValue.GetValue(),
                    createdBy);
            }

            AddHistory($"Création de la version {newVersion.Version}", createdBy);

            return newVersion;
        }

        public void RestoreVersion(CustomItem versionToRestore, string restoredBy)
        {
            if (versionToRestore.Id == Id)
                throw new InvalidOperationException("Impossible de restaurer la version courante");

            if (versionToRestore.CustomEntityId != CustomEntityId)
                throw new InvalidOperationException("Les Custom Types ne correspondent pas");

            foreach (var fieldValue in versionToRestore.FieldValues)
            {
                SetFieldValue(
                    fieldValue.CustomField.SystemName,
                    fieldValue.GetValue(),
                    restoredBy);
            }

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = restoredBy;

            AddHistory($"Restauration depuis la version {versionToRestore.Version}", restoredBy);
        }

        public void Delete(string deletedBy)
        {
            if (Status == CustomItemStatus.Published)
                throw new InvalidOperationException("Impossible de supprimer un item publié. Dépublié-le d'abord.");

            Status = CustomItemStatus.Deleted;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = deletedBy;

            AddHistory("Suppression de l'item", deletedBy);
        }

        public void Restore(string restoredBy)
        {
            if (!IsDeleted)
                throw new InvalidOperationException("L'item n'est pas supprimé");

            Status = CustomItemStatus.Draft;
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

            var existingValue = FieldValues.FirstOrDefault(fv => fv.CustomField.SystemName == fieldSystemName);

            if (existingValue == null)
            {
                var fieldValue = new FieldValue(this, field, value);
                FieldValues.Add(fieldValue);
            }
            else
            {
                existingValue.SetValue(value);
            }

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory($"Mise à jour du champ '{field.Name}'", updatedBy);
        }

        public object? GetFieldValue(string fieldSystemName)
        {
            var fieldValue = FieldValues.FirstOrDefault(fv => fv.CustomField.SystemName == fieldSystemName);

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

        /// <summary>
        /// Calcule le slug en respectqant les règles de transformation suivantes :
        /// 1. Les majusculs desvienent des minuscules (ToLowerInvariant())
        /// 2. Les espaces deviennent des tirets (Replace(" ", "-"))
        /// 3. Supprimer les caractères spéciaux (apostrophes, guillemets)
        /// 4. Nettoyage (garder seulement lettres, chiffres, tirets)
        /// 5. Réduire les tirets multiples (--- → -)
        /// 6. Supprimer les tirets de début/fin
        /// </summary>
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
            CustomItem relatedItem,
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
                var relation = new CustomItemRelation(
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

        public void RemoveRelation(CustomItem relatedItem, string relationType, string updatedBy)
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

        public List<CustomItem> GetRelatedItems(string relationType)
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
            CustomEntity CustomEntity,
            string displayName,
            string createdBy)
        {
            if (CustomEntity == null)
                throw new ArgumentNullException(nameof(CustomEntity));

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Le nom d'affichage est requis", nameof(displayName));

            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("L'utilisateur créateur est requis", nameof(createdBy));

            if (CustomEntity.Status != CustomEntityStatus.Active)
                throw new InvalidOperationException($"Le Custom Type '{CustomEntity.Name}' n'est pas actif");
        }

        private void AddHistory(string description, string changedBy)
        {
            History.Add(new CustomItemHistory
            {
                CustomItem = this,
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
            return Status == CustomItemStatus.Draft && !IsDeleted;
        }

        public bool IsPublished()
        {
            return Status == CustomItemStatus.Published;
        }

        public CustomItemSummary GetSummary()
        {
            return new CustomItemSummary
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

        // A mettre dans le contrôleur Blazor/API
        // [HttpGet("items/{slug}")]
        // public async Task<IActionResult> GetItemBySlug(string slug)
        // {
        //     var item = await dbContext.CustomItems
        //         .FirstOrDefaultAsync(i => i.Slug == slug);
        // 
        //     if (item == null) return NotFound();
        //     return Ok(item);
        // }
    }
}