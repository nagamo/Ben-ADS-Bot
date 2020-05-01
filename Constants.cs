using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1
{
    public class Constants
    {
        public const string Event_Help = "help";

        public const string Event_Cancel = "cancel";

        public const string Event_Card = "card";

        public const string Event_Card_Submit = "card_submit";

        public const string Event_Interest = "interest";

        public static string[] HelpOptions = new string[]
        {
            "Explore Financing",
            "Identify a Vehicle",
            "Value a Trade-In",
            "Search Inventory"
        };
    }
}
