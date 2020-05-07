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



        public const string HELP_Financing = "Explore Financing";
        //public const string HELP_Identify = "Identify a Vehicle";
        public const string HELP_TradeIn = "Value a Trade-In";
        public const string HELP_Inventory = "Search Inventory";

        public static string[] HelpOptions = new string[]
        {
            HELP_Financing,
            //HELP_Identify,
            HELP_TradeIn,
            HELP_Inventory
        };

        public static Dictionary<string, string> DialogEventTriggers = new Dictionary<string, string>()
        {
            {"financing", HELP_Financing },
            //{"identify", HELP_Identify },
            {"tradein", HELP_TradeIn },
            {"inventory", HELP_Inventory }
        };
    }
}
