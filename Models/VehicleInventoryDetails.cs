using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    public class VehicleInventoryDetails : IADSCRMRecord
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(NewUsed) &&
                    !string.IsNullOrEmpty(PrimaryConcern) &&
                    (
                        !string.IsNullOrEmpty(Make) ||
                        !string.IsNullOrEmpty(Model) ||
                        !string.IsNullOrEmpty(Color) ||
                        MinPrice.HasValue ||
                        MaxPrice.HasValue
                    );
            }
        }

        public long? ADS_CRM_ID { get; set; } = null;

        //This one is always asked
        public string NewUsed { get; set; } = null;

        //All car params
        public string Make { get; set; } = null;
        public string Model { get; set; } = null;
        public string Color { get; set; } = null;
        public int? MinPrice { get; set; } = null;
        public int? MaxPrice { get; set; } = null;

        public static string[] AskableFields = new string[]
        {
            "Max Price", //Bit of a hack...
            "Price Range", //Bit of a hack...
            nameof(Make),
            nameof(Model),
            nameof(Color)
        };
        public List<string> MissingFields()
        {
            var missing = new List<string>();
            if (!MinPrice.HasValue && !MaxPrice.HasValue)
            {
                missing.Add("Max Price");
                missing.Add("Price Range");
            }
            if (string.IsNullOrEmpty(Make)) missing.Add(nameof(Make));
            //Only show Model as an option when Make is filled out
            if (!string.IsNullOrEmpty(Make) && string.IsNullOrEmpty(Model)) missing.Add(nameof(Model));
            if (string.IsNullOrEmpty(Color)) missing.Add(nameof(Color));
            return missing;
        }

        public void ParsePrices(string Input)
        {
            var enteredPrices = Regex.Matches(Input, @"([<]*\$?[<]*(\d+)[kK\+]*)");

            MaxPrice = MinPrice = null;

            if (enteredPrices.Count == 1)
            {
                var priceFilter = enteredPrices.First();
                var thousands = priceFilter.Value.Contains("k", StringComparison.OrdinalIgnoreCase);
                if (Input.Contains("<")) { MaxPrice = int.Parse(priceFilter.Groups[2].Value) * (thousands ? 1000 : 1); }
                else if (Input.Contains("+")) { MinPrice = int.Parse(priceFilter.Groups[2].Value) * (thousands ? 1000 : 1); }
            }
            else if (enteredPrices.Count >= 2)
            {
                //Bit ugly, but it works :)
                MinPrice = int.Parse(enteredPrices[0].Groups[2].Value) * (enteredPrices[0].Value.Contains("k", StringComparison.OrdinalIgnoreCase) ? 1000 : 1);
                MaxPrice = int.Parse(enteredPrices[1].Groups[2].Value) * (enteredPrices[1].Value.Contains("k", StringComparison.OrdinalIgnoreCase) ? 1000 : 1);
            }
        }

        public string PrimaryConcern { get; set; }

        [JsonIgnore]
        public bool SkipNewUsed
        { get { return !string.IsNullOrEmpty(NewUsed); } }

        [JsonIgnore]
        public bool SkipPrimaryConcern
        { get { return !string.IsNullOrEmpty(PrimaryConcern); } }
    }
}
