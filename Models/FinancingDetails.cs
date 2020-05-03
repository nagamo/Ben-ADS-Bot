using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
