using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    public class FinancingDetails
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(CreditScore) &&
                    !string.IsNullOrEmpty(Income) &&
                    !string.IsNullOrEmpty(HomeOwnership) &&
                    !string.IsNullOrEmpty(Employment);
            }
        }

        public string CreditScore { get; set; }
        public string Employment { get; set; }
        public string Income { get; set; }
        public string HomeOwnership { get; set; }

        [JsonIgnore]
        public bool SkipCreditScore
        {
            get
            {
                return !string.IsNullOrEmpty(CreditScore);
            }
        }

        //This one is actually probably worth writing out for other uses, so no [JsonIgnore]
        public bool GoodCredit
        {
            get
            {
                if (string.IsNullOrEmpty(CreditScore)) return false;

                //Handle arbitrary user input
                if (int.TryParse(CreditScore, out var score))
                    return score >= 700;

                //Match the first occurence of a 3-digit sequence
                var searchMatch = Regex.Match(CreditScore, @"(\d{3})");
                if(searchMatch.Success && searchMatch.Groups.Count == 2)
                {
                    return int.Parse(searchMatch.Groups[1].Value) >= 700;
                }

                //TODO: Should refer directly to the list of credit options
                //This should be handled by the 
                else return CreditScore == "700+";
            }
        }

        [JsonIgnore]
        public bool SkipEmployment
        {
            get
            {
                return !string.IsNullOrEmpty(Employment) || GoodCredit;
            }
        }

        [JsonIgnore]
        public bool SkipIncome
        {
            get
            {
                return !string.IsNullOrEmpty(Employment) || GoodCredit;
            }
        }

        [JsonIgnore]
        public bool SkipHome
        {
            get
            {
                return !string.IsNullOrEmpty(Employment) || GoodCredit;
            }
        }
    }
}
