using ADS.Bot1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    public class FinancingDetails : IADSCRMRecord
    {
        public bool IsCompleted
        {
            get
            {
                return CreditEntered && EmploymentEntered && IncomeEntered && HomeEntered;
            }
        }

        public long? ADS_CRM_ID { get; set; } = null;

        public string CreditScore { get; set; }
        public string Employment { get; set; }
        public string Income { get; set; }
        public string HomeOwnership { get; set; }

        [JsonIgnore]
        public bool CreditEntered
        {
            get
            {
                return !string.IsNullOrEmpty(CreditScore);
            }
        }

        //This one is actually probably worth writing out for other uses, so no [JsonIgnore]
        public int CreditValue
        {
            get
            {
                if (string.IsNullOrEmpty(CreditScore)) return 0;

                //Handle arbitrary user input
                if (int.TryParse(CreditScore, out var score))
                    return score;

                //Match the first occurence of a 3-digit sequence
                var searchMatch = Regex.Match(CreditScore, @"(\d{3})");
                if(searchMatch.Success && searchMatch.Groups.Count == 2)
                {
                    return int.Parse(searchMatch.Groups[1].Value);
                }

                //TODO: Should refer directly to the list of credit options
                //This should be handled by the 
                else return 0;
            }
        }

        [JsonIgnore]
        public bool EmploymentEntered
        {
            get
            {
                return !string.IsNullOrEmpty(Employment);
            }
        }

        [JsonIgnore]
        public bool IncomeEntered
        {
            get
            {
                return !string.IsNullOrEmpty(Income);
            }
        }

        [JsonIgnore]
        public bool HomeEntered
        {
            get
            {
                return !string.IsNullOrEmpty(HomeOwnership);
            }
        }

        public async Task<bool> IsValidForSubmit(UserProfile Profile, ADSBotServices Services)
        {
            return 
                await Services.DealerConfig.IsValidResponseAsync(Profile, Profile.Financing.CreditScore, "finance.credit") &&
                await Services.DealerConfig.IsValidResponseAsync(Profile, Profile.Financing.Employment, "finance.employment") &&
                await Services.DealerConfig.IsValidResponseAsync(Profile, Profile.Financing.Income, "finance.income") &&
                await Services.DealerConfig.IsValidResponseAsync(Profile, Profile.Financing.HomeOwnership, "finance.home");
        }
    }
}
