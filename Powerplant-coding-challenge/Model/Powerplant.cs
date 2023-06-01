namespace Powerplant_coding_challenge.Model
{
    public class Powerplant
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double Efficiency { get; set; }
        public int PMin { get; set; }
        public int PMax { get; set; }
        public double PricePerMWH { get; set; }
    }
}
