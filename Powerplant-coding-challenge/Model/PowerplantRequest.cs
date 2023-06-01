using Newtonsoft.Json;

namespace Powerplant_coding_challenge.Model
{
    public class PowerplantRequest
    {
        [JsonProperty("load")]
        public int Load { get; set; }
        [JsonProperty("fuels")]
        public Fuels Fuels { get; set; }
        [JsonProperty("powerplants")]
        public List<Powerplant> Powerplants { get; set; } = new List<Powerplant>();
    }
}
