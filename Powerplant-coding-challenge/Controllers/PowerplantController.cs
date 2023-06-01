using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Powerplant_coding_challenge.Model;

namespace Powerplant_coding_challenge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PowerplantController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> CalculatePowerplantResult()
        {
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                var request = JsonConvert.DeserializeObject<PowerplantRequest>(await reader.ReadToEndAsync());
                if (request == null)
                {
                    Console.WriteLine("Could not parse body");
                    return new JsonResult(BadRequest("Could not parse body"));
                }
                try
                {
                    //Calculate the Price per MWh
                    CalculatePricePerMWH(request);
                    var response = CalculateResponse(request);
                    return Ok(JsonConvert.SerializeObject(response));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occured: {ex.Message}. Stacktrace : {ex.StackTrace}");
                    return new JsonResult(BadRequest("Unexpected error occured"));
                }
            }
        }
        public static double AddPowerplant(double powerProduced, Powerplant plant, List<PowerplantResponse> responses, List<Powerplant> powerplants)
        {
            responses.Add(new PowerplantResponse(plant.Name, powerProduced, plant));
            powerplants.Remove(plant);
            return powerProduced;

        }
        public static List<PowerplantResponse> CalculateResponse(PowerplantRequest request)
        {

            var orderedPowerplants = request.Powerplants.OrderBy(x => x.PricePerMWH).ThenByDescending(x => x.PMax).ToList();
            List<PowerplantResponse> responses = new List<PowerplantResponse>();
            double totalPower = 0;
            while (orderedPowerplants.Any())
            {
                var powerplant = orderedPowerplants.First();
                if (totalPower == request.Load)
                {
                    AddPowerplant(0, powerplant, responses, orderedPowerplants);
                    continue;
                }

                var maxPowerProducedByPlant = CalculateMaxPowerProduced(powerplant, request.Fuels.Wind);

                if (totalPower + maxPowerProducedByPlant <= request.Load)
                {
                    totalPower += AddPowerplant(maxPowerProducedByPlant, powerplant, responses, orderedPowerplants);
                    continue;
                }
                // this could be optimized by selecting the most efficient combination of windturbines, but i'm nearing the time limit of 4 hours.
                if (powerplant.Type == "windturbine")
                {
                    AddPowerplant(0, powerplant, responses, orderedPowerplants);
                    continue;
                }
                if (totalPower + powerplant.PMin <=  request.Load)
                {
                    totalPower += AddPowerplant(request.Load - totalPower, powerplant, responses, orderedPowerplants);
                    continue;
                }

                var plantsThatCanMaxOut = orderedPowerplants.Where(x => totalPower + x.PMin <=  request.Load && totalPower + x.PMax >= request.Load).OrderBy(x => x.PricePerMWH).ToList();
                //here you could also calculate in turning entire plants off, which would have a set fluctuation, but i'm nearing the time limit of 4 hours.
                var plantsThatCanFluctuate = responses.Where(x => x.Plant.Type != "windturbine").OrderByDescending(x => x.Plant.PricePerMWH).ToList();
                var howMuchCanWeFluctuate = plantsThatCanFluctuate.Sum(x => x.Power - x.Plant.PMin);
                if (totalPower - howMuchCanWeFluctuate + powerplant.PMin <= request.Load)
                {
                    //check if there is a powerplant available that can fullfill the load with a lower cost than changing the poweroutput of other plants
                    // This could be more optimized by calculating the correct cost instead of the minimum, but i'm nearing the time limit of 4 hours.

                    if (plantsThatCanMaxOut.Any())
                    {
                        var CostForSmallerPlantToRun = (request.Load - totalPower) * plantsThatCanMaxOut.First().PricePerMWH;
                        var minimumCost = powerplant.PMin * powerplant.PricePerMWH - ((totalPower + powerplant.PMin - request.Load) * plantsThatCanFluctuate.First().Plant.PricePerMWH);

                        if (minimumCost > CostForSmallerPlantToRun)
                        {
                            totalPower += AddPowerplant(request.Load - totalPower, plantsThatCanMaxOut.First(), responses, orderedPowerplants);
                            continue;
                        }
                    }
                    while (plantsThatCanFluctuate.Any())
                    {
                        var fluctuatingPlant = plantsThatCanFluctuate.First();
                        var possibleFluctuation = fluctuatingPlant.Power - fluctuatingPlant.Plant.PMin;
                        if (totalPower - possibleFluctuation + powerplant.PMin <= request.Load)
                        {
                            var powerThePlantShouldReduce = (totalPower + powerplant.PMin) - request.Load;
                            fluctuatingPlant.Power -= powerThePlantShouldReduce;
                            totalPower -= powerThePlantShouldReduce;
                            break;
                        }
                        fluctuatingPlant.Power -= possibleFluctuation;
                        totalPower -= possibleFluctuation;
                        plantsThatCanFluctuate.Remove(fluctuatingPlant);

                    }

                    totalPower += AddPowerplant(powerplant.PMin, powerplant, responses, orderedPowerplants);
                    continue;
                }

                // if we were not able to add the powerplant to the list, then don't start it up and look for smaller plants that can fulfill the need of the load
                AddPowerplant(0, powerplant, responses, orderedPowerplants);
            }
            return responses;
        }


        public static double CalculateMaxPowerProduced(Powerplant plant, int wind)
        {
            if (plant.Type == "windturbine")
            {
                return wind*plant.PMax/100;
            }
            return plant.PMax;
        }
        public static void CalculatePricePerMWH(PowerplantRequest request)
        {
            foreach (var powerplant in request.Powerplants)
            {

                switch (powerplant.Type)
                {
                    case "gasfired":
                        powerplant.PricePerMWH = request.Fuels.Gas / powerplant.Efficiency + 0.3 *request.Fuels.Co2;
                        break;
                    case "turbojet":// The assignment only mentioned gas generated CO2, so the CO2 is not calculated in for turbojet
                        powerplant.PricePerMWH = request.Fuels.Kerosine / powerplant.Efficiency;
                        break;
                    case "windturbine":
                        powerplant.PricePerMWH = 0;
                        break;
                }

            }
        }


    }
}
