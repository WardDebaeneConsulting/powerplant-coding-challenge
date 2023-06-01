using Newtonsoft.Json;

namespace Powerplant_coding_challenge.Model
{
    public class PowerplantResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("p")]
        public double Power { get; set; }
        [JsonIgnore]
        public Powerplant Plant { get; set; }

        public PowerplantResponse(string name, double power, Powerplant plant )
        {
            Name = name;
            Power = power;
            Plant = plant;
        }
    }
}
