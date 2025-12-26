namespace FlexiBase.Models
{
    public class DecimalFieldConfiguration
    {
        public int Precision { get; set; } = 18;
        public int Scale { get; set; } = 2;
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Currency { get; set; }
    }
}
