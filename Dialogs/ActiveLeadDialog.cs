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
using System.Threading;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using ADS.Bot.V1.Cards;
using Newtonsoft.Json.Linq;
using ADS.Bot.V1.Models;

namespace ADS.Bot.V1.Dialogs
{
    public class ActiveLeadDialog : ComponentDialog
    {

        List<ICardFactory> CardFactories = new List<ICardFactory>();
        public ADSBotServices Services { get; }



        public ActiveLeadDialog(
            UserProfileDialog userProfileDialog,
            FinanceDialog financeDialog,
            VehicleProfileDialog vehicleProfileDialog,
            ValueTradeInDialog valueTradeInDialog,
            SimpleInventoryDialog inventoryDialog,
            ICardFactory<BasicDetails> profileFactory,
            ICardFactory<FinancingDetails> financeFactory,
            ICardFactory<TradeInDetails> tradeinFactory,
            ICardFactory<VehicleProfileDetails> vehicleFactory,
            ADSBotServices botServices) 
            : base(nameof(ActiveLeadDialog))
        {
            Services = botServices;

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(),
                Triggers = new List<OnCondition>()
                {
                    new OnCustomEvent(Constants.Event_Help)
                    {
                        Actions = new List<Dialog>()
                        {
                            new SetProperty()
                            {
                                Property = "conversation.seen_help",
                                Value = "true"
                            },
                            new CodeAction(SendHelp),
                            new SetProperty()
                            {
                                Property = "conversation.interest",
                                Value = "turn.activity.text"
                            },
                            new IfCondition()
                            {
                                Condition = "conversation.interest != null",
                                Actions = new List<Dialog>()
                                {
                                    new EmitEvent(Constants.Event_Interest)
                                }
                            },
                        }
                    },
                    new OnCustomEvent(Constants.Event_Interest)
                    {
                        Condition = "conversation.interest != null",
                        Actions = new List<Dialog>()
                        {
                            new IfCondition()
                            {
                                Condition = "user.UserProfile.IsRegistered",
                                Actions = new List<Dialog>()
                                {
                                    new SwitchCondition()
                                    {
                                        Condition = "conversation.interest",
                                        Cases = new List<Case>()
                                        {
                                            new Case() {
                                                Value = Constants.INTEREST_Financing,
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'financing'") }
                                            },
                                            new Case() {
                                                Value = Constants.INTEREST_Identify,
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'vehicle'") }
                                            },
                                            new Case() {
                                                Value = Constants.INTEREST_TradeIn,
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'tradein'") }
                                            },
                                            new Case() {
                                                Value = Constants.INTEREST_Inventory,
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'inventory'") }
                                            }
                                        }
                                    }
                                },
                                ElseActions = new List<Dialog>()
                                {
                                    new SetProperty()
                                    {
                                        Property = "conversation.residual_interest",
                                        Value = "conversation.interest"
                                    },
                                    new DeleteProperty()
                                    {
                                        Property = "conversation.interest"
                                    },
                                    new EmitEvent(Constants.Event_Card, "'profile'")
                                }
                            },
                            //Delete the property so we don't loop forever
                            new DeleteProperty()
                            {
                                Property = "conversation.interest"
                            }
                        }
                    },
                    new OnCustomEvent(Constants.Event_Cancel)
                    {
                        Actions = new List<Dialog>()
                        {
                            //  when the user cancels we need to check where they were so that we can transition smoothly.
                            //  what if they weren't in a dialog? What if they've typed 'cancel' twice in a row?
                            new EmitEvent(Constants.Event_Help, bubble: true),
                            new CancelAllDialogs(),
                        }
                    },
                    new OnCustomEvent(Constants.Event_Card)
                    {
                        Actions = new List<Dialog>()
                        {
                            new SetProperty()
                            {
                                Property = "conversation.busy",
                                Value = "true"
                            },
                            new SwitchCondition()
                            {
                                Condition = "toLower(turn.dialogEvent.value)",
                                Cases = new List<Case>()
                                {
                                    //Just copied from below as a quick fix, ideally this would all be in the financing dialog itself.
                                    new Case("profile")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            new BeginDialog(userProfileDialog.Id)
                                        }
                                    },
                                    new Case("financing")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            new BeginDialog(financeDialog.Id)
                                        }
                                    },
                                    new Case("vehicle")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            new BeginDialog(vehicleProfileDialog.Id)
                                        }
                                    },
                                    new Case("tradein")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            new BeginDialog(valueTradeInDialog.Id)
                                        }
                                    },
                                    new Case("inventory")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            new BeginDialog(inventoryDialog.Id)
                                        }
                                    },
                                },
                                Default = new List<Dialog>()
                                {
                                    new SendActivity("I'm sorry, I can't handle that request yet. :("),
                                    new EmitEvent(Constants.Event_Help, bubble: true)
                                }
                            },
                            new SetProperty()
                            {
                                Property = "conversation.busy",
                                Value = "false"
                            },
                            new IfCondition()
                            {
                                Condition = "conversation.residual_interest != null",
                                Actions = new List<Dialog>()
                                {
                                    new SetProperty()
                                    {
                                        Property = "conversation.interest",
                                        Value = "conversation.residual_interest"
                                    },
                                    new DeleteProperty()
                                    {
                                        Property = "conversation.residual_interest"
                                    },
                                    new EmitEvent(Constants.Event_Interest)
                                },
                                ElseActions = new List<Dialog>()
                                {
                                    new DeleteProperty()
                                    {
                                        Property = "conversation.interest"
                                    },
                                    new EmitEvent(Constants.Event_Help)
                                }
                            },
                        }
                    },

                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
#if DEBUG
                            new TraceActivity(){Name = "OnBeginDialog"},
#endif
                            new CodeAction(async (context, _)=>{
                                await Services.SetUserProfileAsync(await Services.GetUserProfileAsync(context.Context), context);
                                return new DialogTurnResult(DialogTurnStatus.Complete);
                            }),
                            new EmitEvent(Constants.Event_Help),
                        }
                    },
                    new OnMessageActivity()
                    {
                        Actions = new List<Dialog>()
                        {
#if DEBUG
                            new TraceActivity("OnMessageActivity"){Name = "OnMessageActivity"},
#endif
                            new CodeAction(async (context, _)=>{
                                await Services.SetUserProfileAsync(await Services.GetUserProfileAsync(context.Context), context);
                                return new DialogTurnResult(DialogTurnStatus.Complete);
                            }),
                            new CodeAction(PrimaryHandler)
                        }
                    }
                }
            };

            CardFactories.Add(profileFactory);
            CardFactories.Add(financeFactory);
            CardFactories.Add(tradeinFactory);
            CardFactories.Add(vehicleFactory);

            AddDialog(rootDialog);

            AddDialog(userProfileDialog);
            AddDialog(financeDialog);
            AddDialog(vehicleProfileDialog);
            AddDialog(valueTradeInDialog);
            AddDialog(inventoryDialog);

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        }

        public async Task<DialogTurnResult> DispalyHelp(DialogContext context, object data)
        {
            await context.Context.SendActivityAsync(MessageFactory.SuggestedActions(Constants.HelpOptions));
            //await context.Context.SendActivityAsync("Test");

            return new DialogTurnResult(DialogTurnStatus.Waiting, null);
        }

        public async Task<DialogTurnResult> PrimaryHandler(DialogContext context, object data)
        {
            bool isBusy = context.GetState().GetValue<bool>("conversation.busy", () => false);

            //Always hit LUIS first
            if(Services.LuisRecognizer != null)
            {
                var recognizerResult = await Services.LuisRecognizer.RecognizeAsync(context.Context, CancellationToken.None);

                foreach(var checkIntent in recognizerResult.Intents)
                {
                    if (Constants.IntentThresholds.ContainsKey(checkIntent.Key) && checkIntent.Value.Score >= Constants.IntentThresholds[checkIntent.Key])
                    {
                        return await PerformIntent(checkIntent.Key, recognizerResult, context);
                    }
                }
            }


            if (Services.LeadQualQnA != null)
            {
                if(Constants.HelpOptions.Contains(context.Context.Activity.Text))
                {
                    //await context.EmitEventAsync(Constants.Event_Interest);
                    return new DialogTurnResult(DialogTurnStatus.CompleteAndWait);
                }
                else
                {
                    return await ProcessDefaultResponse(context, data, isBusy);
                }
            }

            //You can change status to alter the behaviour post-completion
            return new DialogTurnResult(DialogTurnStatus.Waiting, null);
        }

        public async Task<DialogTurnResult> PerformIntent(string Intent, RecognizerResult result, DialogContext context)
        {
            switch (Intent)
            {
                case "CheckInventory":
                    await SendInterest(context, Constants.INTEREST_Inventory);
                    break;
                case "FindVehicle":
                    await SendInterest(context, Constants.INTEREST_Inventory);
                    break;
                case "GetFinanced":
                    await SendInterest(context, Constants.INTEREST_Financing);
                    break;
                case "Utilities_Cancel":
                    await context.EmitEventAsync(Constants.Event_Cancel);
                    break;
                case "Utilities_GoBack":
                    await context.EmitEventAsync(Constants.Event_Cancel);
                    break;
                case "Utilities_Reject":
                    await context.EmitEventAsync(Constants.Event_Reject);
                    break;
                case "ValueTrade":
                    await SendInterest(context, Constants.INTEREST_TradeIn);
                    break;
                default:
                    //New intent?
                    break;
            }

            return new DialogTurnResult(DialogTurnStatus.Complete, null);
        }

        public async Task<DialogTurnResult> ProcessDefaultResponse(DialogContext context, object data, bool isBusy)
        {
            if(context.Context.Activity.Text == null)
            {
                //This is set null when we want to ignore LUIS.
                //Waiting because its sent as a prompt.
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }

            //Get Top QnA result
            var results = await Services.LeadQualQnA.GetAnswersAsync(context.Context, Services.QnAOptions);
            var topResult = results.FirstOrDefault();


            if (topResult != null && topResult.Score > 0.75)
            {
                //Convert Metadata tags to dictionary for comparison
                var resultTags = topResult.Metadata.ToDictionary(m => m.Name.ToLower(), m => m.Value);
                if(resultTags.ContainsKey("event"))
                {
                    //emit arbitrary events based on an "event" metadata record
                    await context.EmitEventAsync(resultTags["event"]);
                }
                else if(resultTags.ContainsKey("card"))
                {
                    //emit card display event, based on the value of the "card" tag, if present
                    //this causes the card to be displayed "independently" through the custom event handler
                    if (Constants.DialogEventTriggers.ContainsKey(resultTags["card"]))
                    {
                        //Remap the card event value from it's shorthand form, so we can reuse the residual interest logic
                        //eg. ('financing') to full name ('Explore Financing')
                        await SendInterest(context, Constants.DialogEventTriggers[resultTags["card"]]);
                    }
                }

                //We always send the response from QnA
                await context.Context.SendActivityAsync(MessageFactory.Text(topResult.Answer));

                //If we aren't in a dialog, just always re-prompt the user with help dialog.
                if (!isBusy)
                {
                    await context.EmitEventAsync(Constants.Event_Help);
                }

                //and since the user has asked for a specific intent, we don't wait for further input. Call
                //the turn complete
                return new DialogTurnResult(DialogTurnStatus.Complete);
            }
            else
            {
                
                if (isBusy)
                {
                    //If we are in a dialog, we want to 'Complete' to resume the let the dialog itself take control
                    return new DialogTurnResult(DialogTurnStatus.Complete);
                }
                else
                {
                    //Otherwise a user entered an invalid root-level


                    if (topResult != null)
                    {
                        Console.WriteLine($"Rejected low-confidence response to text '{context.Context.Activity.Text}' - [{topResult.Score}] {topResult.Source}:'{topResult.Answer}'");
                    }

                }
            }

            await context.Context.SendActivityAsync(MessageFactory.Text("I'm not quite sure what you meant..."));
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        public async Task<DialogTurnResult> Test(DialogContext context, object something)
        {
            return new DialogTurnResult(DialogTurnStatus.Complete);
        }


        public async Task<DialogTurnResult> SendHelp(DialogContext context, object something)
        {
            var helpOptions = Utilities.CreateOptions(Constants.HelpOptions, (string)null, null, ListStyle.SuggestedAction);

            return await context.PromptAsync(nameof(ChoicePrompt), helpOptions);
        }


        private async Task SendInterest(DialogContext Context, string InterestName)
        {
            Context.GetState().SetValue("conversation.interest", InterestName);
            await Context.EmitEventAsync(Constants.Event_Interest);
        }
    }
}
