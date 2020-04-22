using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot1.Dialogs
{
    public class InventoryDialog : WaterfallDialog
    {
        public InventoryDialog(string dialogId, IEnumerable<WaterfallStep> steps = null) : base(dialogId, steps)
        {

        }

        public static string Id => "inventoryDialog";

        public static InventoryDialog Instance { get; } = new InventoryDialog(Id);

    }
}
