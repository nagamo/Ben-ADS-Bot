using ADS.Bot1;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.LanguageGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ADS.Bot1.Dialogs;

namespace ADS.Bot.V1.Dialogs
{
    public class ActiveLeadDialog : ComponentDialog
    {
        private readonly UserState _userState;

        public ActiveLeadDialog(UserState userState, VehicleProfileDialog vehicleDialog, ValueTradeInDialog tradeInDialog, IBotServices botServices) 
            : base(nameof(ActiveLeadDialog))
        {
            _userState = userState;

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(),
                Recognizer = botServices.Dispatch,
                Triggers = new List<OnCondition>()
                {
                    new OnIntent("Greeting")
                    {
                        // this needs to be combined with a 'restart' intent, where the text sent to the user recognizes
                        // that they're cycling back through everything.
                        Condition = "#Greeting.Score >= 0.9",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Hello!")
                        }
                    },
                    new OnIntent("Financing")
                    {
                        // again, are they coming back, or is this their first visit?
                        Condition = "#Financing.Score >= 0.8",
                        Actions = new List<Dialog>()
                        {
                            new BeginDialog(nameof(FinanceDialog))
                        }
                    },
                    new OnIntent("Vehicle")
                    {
                        Condition = "#Vehicle.Score >= 0.8",
                        Actions = new List<Dialog>()
                        {
                            new BeginDialog(nameof(VehicleProfileDialog))
                        }
                    },
                    new OnIntent("Help")
                    {
                        Condition = "#Help.Score >= 0.8",
                        Actions = new List<Dialog>()
                        {
                            // this should display a card that gives them some explanations and a set of discrete selections.
                            new SendActivity("Sure! I'll get some help here eventually.")
                        }
                    },
                    new OnIntent("Cancel")
                    {
                        Condition = "#Cancel.Score >= 0.8",
                        Actions = new List<Dialog>()
                        {
                            // when the user cancels we need to check where they were so that we can transition smoothly.
                            // what if they weren't in a dialog? What if they've typed 'cancel' twice in a row?
                            new SendActivity("Really? You want to quit? And we were having so much fun! But have it your way."),
                            new CancelAllDialogs()
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("OK, you totally lost me! Give it another shot, will ya??")
                        }
                    }
                }
            };

            AddDialog(rootDialog);
            AddDialog(new FinanceDialog(_userState));
            AddDialog(vehicleDialog);
            AddDialog(tradeInDialog);
        }

    }
}
