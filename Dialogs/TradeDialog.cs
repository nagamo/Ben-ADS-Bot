using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot1.Dialogs
{
    public class TradeDialog : WaterfallDialog
    {
        public TradeDialog(string dialogId, IEnumerable<WaterfallStep> steps = null) : base(dialogId, steps)
        {

        }

        public static string Id => "tradeDialog";

        public static TradeDialog Instance { get; } = new TradeDialog(Id);

    }
}
