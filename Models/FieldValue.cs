using FlexiBase.Models.enums;
using FlexiBase.Models.interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Représente la valeur d'un champ pour un Content Item
    /// </summary>
    [Table("FieldValues")]
    public class FieldValue : IAuditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        public int ValueVersion { get; private set; } = 1;

        // Propriétés de stockage EAV
        [Column(TypeName = "nvarchar(max)")]
        public string? StringValue { get; private set; }

        public long? LongValue { get; private set; }

        [Column(TypeName = "decimal(28, 10)")]
        public decimal? DecimalValue { get; private set; }

        public bool? BooleanValue { get; private set; }

        public DateTime? DateTimeValue { get; private set; }

        [MaxLength(1000)]
        public string? FilePath { get; private set; }

        public int? RelationValue { get; private set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? JsonValue { get; private set; }

        // Relations
        [Required]
        public int CustomItemId { get; private set; }

        [Required]
        public int CustomFieldId { get; private set; }

        // Propriétés d'audit
        [Required]
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; private set; } = string.Empty;

        public DateTime? UpdatedAt { get; private set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; private set; }

        [Timestamp]
        public byte[]? RowVersion { get; private set; }

        // Navigation properties
        [ForeignKey(nameof(CustomItemId))]
        public virtual CustomItem CustomItem { get; private set; } = null!;

        [ForeignKey(nameof(CustomFieldId))]
        public virtual CustomField CustomField { get; private set; } = null!;

        // Constructeur protégé pour EF Core
        protected FieldValue() { }

        // Constructeur principal
        public FieldValue(
            CustomItem customItem,
            CustomField customField,
            object? value,
            string createdBy = "")
        {
            CustomItem = customItem ?? throw new ArgumentNullException(nameof(customItem));
            CustomItemId = customItem.Id;
            CustomField = customField ?? throw new ArgumentNullException(nameof(customField));
            CustomFieldId = customField.Id;
            CreatedBy = createdBy ?? throw new ArgumentException("L'utilisateur créateur est requis", nameof(createdBy));

            SetValue(value);
        }

        /// <summary>
        /// Définit la valeur du champ
        /// </summary>
        public void SetValue(object? value)
        {
            // Réinitialiser toutes les valeurs
            ClearAllValues();

            if (value == null)
                return;

            // Stocker selon le type de champ
            // Note: CustomField.DataType doit être défini dans CustomField
            switch (CustomField.DataType)
            {
                case FieldDataType.String:
                case FieldDataType.Text:
                case FieldDataType.Email:
                case FieldDataType.Url:
                case FieldDataType.Phone:
                case FieldDataType.Color:
                case FieldDataType.Select:
                    StringValue = value.ToString();
                    break;

                case FieldDataType.Integer:
                    if (int.TryParse(value.ToString(), out var intValue))
                        LongValue = intValue;
                    else
                        throw new ArgumentException($"Valeur invalide pour un champ entier: {value}");
                    break;

                case FieldDataType.Decimal:
                    if (decimal.TryParse(value.ToString(), out var decimalValue))
                        DecimalValue = decimalValue;
                    else
                        throw new ArgumentException($"Valeur invalide pour un champ décimal: {value}");
                    break;

                case FieldDataType.Boolean:
                    if (bool.TryParse(value.ToString(), out var boolValue))
                        BooleanValue = boolValue;
                    else
                        throw new ArgumentException($"Valeur invalide pour un champ booléen: {value}");
                    break;

                case FieldDataType.Date:
                case FieldDataType.DateTime:
                    if (DateTime.TryParse(value.ToString(), out var dateValue))
                        DateTimeValue = dateValue;
                    else
                        throw new ArgumentException($"Valeur invalide pour un champ date: {value}");
                    break;

                case FieldDataType.Time:
                    if (TimeSpan.TryParse(value.ToString(), out var timeValue))
                        StringValue = timeValue.ToString();
                    else
                        throw new ArgumentException($"Valeur invalide pour un champ heure: {value}");
                    break;

                case FieldDataType.File:
                case FieldDataType.Image:
                    StringValue = value.ToString(); // Chemin du fichier
                    FilePath = value.ToString();
                    break;

                case FieldDataType.Relation:
                    if (int.TryParse(value.ToString(), out var relationValue))
                        RelationValue = relationValue;
                    else
                        throw new ArgumentException($"Valeur invalide pour un champ relation: {value}");
                    break;

                case FieldDataType.MultiSelect:
                case FieldDataType.Json:
                    JsonValue = value.ToString(); // JSON sérialisé
                    break;

                default:
                    StringValue = value.ToString();
                    break;
            }

            UpdatedAt = DateTime.UtcNow;
            ValueVersion++;
        }

        /// <summary>
        /// Obtient la valeur du champ
        /// </summary>
        public object? GetValue()
        {
            return CustomField.DataType switch
            {
                FieldDataType.String or FieldDataType.Text or FieldDataType.Email or FieldDataType.Url
                    or FieldDataType.Phone or FieldDataType.Color or FieldDataType.Select => StringValue,

                FieldDataType.Integer => LongValue,

                FieldDataType.Decimal => DecimalValue,

                FieldDataType.Boolean => BooleanValue,

                FieldDataType.Date or FieldDataType.DateTime => DateTimeValue,

                FieldDataType.Time => StringValue != null ? TimeSpan.Parse(StringValue) : null,

                FieldDataType.File or FieldDataType.Image => FilePath ?? StringValue,

                FieldDataType.Relation => RelationValue,

                FieldDataType.MultiSelect or FieldDataType.Json => JsonValue,

                _ => StringValue
            };
        }

        /// <summary>
        /// Obtient la valeur typée
        /// </summary>
        public T? GetValue<T>()
        {
            var value = GetValue();

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

        /// <summary>
        /// Réinitialise toutes les valeurs
        /// </summary>
        private void ClearAllValues()
        {
            StringValue = null;
            LongValue = null;
            DecimalValue = null;
            BooleanValue = null;
            DateTimeValue = null;
            FilePath = null;
            RelationValue = null;
            JsonValue = null;
        }
    }
}
