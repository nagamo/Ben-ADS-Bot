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

        public const string Event_Reject = "reject";

        public const string Event_Card = "card";

        public const string Event_Card_Submit = "card_submit";

        public const string Event_Interest = "interest";


        public const string DEALER_CONTAINER = "dealer";
        public const string DEALER_CONFIG_PATH = "config.json";


        public const string MSG_DONT_SEE_IT = "I don’t see it!";

        public const string INTEREST_Financing = "Explore Financing";
        public const string INTEREST_Identify = "Identify a Vehicle";
        public const string INTEREST_TradeIn = "Value a Trade-In";
        public const string INTEREST_Inventory = "Search Inventory";

        public static string[] HelpOptions = new string[]
        {
            INTEREST_Financing,
            //HELP_Identify,
            INTEREST_TradeIn,
            INTEREST_Inventory
        };

        public static Dictionary<string, string> DialogEventTriggers = new Dictionary<string, string>()
        {
            {"financing", INTEREST_Financing },
            //{"identify", HELP_Identify },
            {"tradein", INTEREST_TradeIn },
            {"inventory", INTEREST_Inventory }
        };

        public static Dictionary<string, float> IntentThresholds = new Dictionary<string, float>()
        {
            {"CheckInventory", 0.8f },
            {"FindVehicle", 0.8f },
            {"GetFinanced", 0.8f },
            {"Utilities_Cancel", 0.9f },
            {"Utilities_GoBack", 0.9f },
            {"Utilities_Reject", 0.9f },
            {"ValueTrade", 0.8f },
        };

        public static string FB_STILL_AVAILABLE = "Is this still available?";
        public static string FB_SCHEDULE_VIDEO = "Can I schedule a time to see this over Messenger video call?";
        public static string FB_VEHICLE_HISTORY = "What's the vehicle's history?";
        public static string FB_PREVIOUS_OWNERS= "How many people owned this previously?";
    }
}
