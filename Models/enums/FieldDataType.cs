namespace FlexiBase.Models.enums
{
    /// <summary>
    /// Type de données d'un champ dans un Content Type
    /// </summary>
    public enum FieldDataType
    {
        // Types simples
        String = 0,
        Text = 1,           // Texte long
        Integer = 2,
        Decimal = 3,
        Boolean = 4,
        DateTime = 5,
        Date = 6,
        Time = 7,

        // Types fichiers
        File = 10,
        Image = 11,

        // Types relations
        Relation = 20,      // Lien vers un autre Content Type

        // Types choix
        Select = 30,        // Liste déroulante simple
        MultiSelect = 31,   // Liste multiple

        // Types avancés
        Email = 40,
        Url = 41,
        Phone = 42,
        Color = 43,
        Json = 44,

        // Types calculés
        Calculated = 50,    // Champ calculé
        Formula = 51        // Basé sur une formule
    }
}
