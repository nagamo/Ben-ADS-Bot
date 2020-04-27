using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1
{
    public class Constants
    {
        public const string Event_Help = "Help";

        public const string Event_Cancel = "Cancel";

        public const string Event_Card = "Card";

        public const string Event_Card_Submit = "Card_Submit";

        public const string Event_Interest = "Interst";

        public static string[] HelpOptions = new string[]
        {
            "Explore Financing",
            "Identify a Vehicle",
            "Value a Trade-In",
            "Search Inventory"
        };
    }
}
