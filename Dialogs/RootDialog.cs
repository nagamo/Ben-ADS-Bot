using ADS.Bot1;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Dialogs
{
    public class RootDialog : ComponentDialog
    {
        public RootDialog(ActiveLeadDialog activeLeadDialog, ADSBotServices services)
        {
            Services = services;

            var rootDialog = new AdaptiveDialog(nameof(RootDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(),
                Triggers = new List<OnCondition>()
                {
                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
                            new IfCondition()
                            {
                                Condition = "conversation.seen_help == false || conversation.seen_help == null",
                                Actions = new List<Dialog>()
                                {
                                    new EmitEvent(Constants.Event_Help)
                                }
                            },
                            new BeginDialog(nameof(ActiveLeadDialog))
                        }
                    },
                    new OnCustomEvent(Constants.Event_Help)
                    {
                        Actions = new List<Dialog>()
                        {
                            new SetProperty()
                            {
                                Property = "conversation.seen_help",
                                Value = "true"
                            },
                            new ChoiceInput()
                            {
                                Prompt = new ActivityTemplate("I want to provide you with the best service possible! " +
                                                              "Just select one of the easy-click options below, or " + 
                                                              "type a request directly into the text box."),
                                AlwaysPrompt = true,
                                AllowInterruptions =  "true",
                                Validations = new List<string>(new string[]{ "true" }),
                                Property = "conversation.interest",
                                Choices = new ChoiceSet(new List<Choice>()
                                {
                                    new Choice() { Value = "Explore Financing" },
                                    new Choice() { Value = "Identify a Vehicle" },
                                    new Choice() { Value = "Value a Trade-In" },
                                    new Choice() { Value = "Search Inventory" }
                                })
                            },
                        }
                    }
                }
            };

            AddDialog(rootDialog);
            AddDialog(activeLeadDialog);

            this.InitialDialogId = rootDialog.Id;
        }

        public ADSBotServices Services { get; }
    }
}
