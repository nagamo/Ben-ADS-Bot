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

namespace ADS.Bot.V1.Dialogs
{
    public class ActiveLeadDialog : ComponentDialog
    {

        List<ICardFactory> CardFactories = new List<ICardFactory>();

        public ActiveLeadDialog(
            VehicleProfileDialog vehicleDialog, 
            ValueTradeInDialog tradeInDialog, 
            FinanceDialog financeDialog, 
            UserProfileDialog userProfileDialog, 
            ProfileCardFactory profileFactory,
            IBotServices botServices) 
            : base(nameof(ActiveLeadDialog))
        {
            Services = botServices;

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(),
                Triggers = new List<OnCondition>()
                {
                    //*From Ben*
                    //  This is an intent from the user, not the bot's intent to send a hello message.
                    //  We actually don't need this from LUIS because QnA will handle cases where a users says anything along the lines of a greeting
                    /*
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
                    */
                    new OnBeginDialog()
                    {
                        //This property is set by RootDialog, based on option selected from help cards
                        Condition = "turn.interest != null",
                        Actions = new List<Dialog>()
                        {
                            new EmitEvent()
                            {
                                EventName = Constants.Event_Card,
                                EventValue = "turn.interest"
                            },
                            //Delete the property so we don't loop forever
                            new DeleteProperty()
                            {
                                Property = "turn.interest"
                            }
                        }
                    },
                    new OnCustomEvent(Constants.Event_Cancel)
                    {
                        Actions = new List<Dialog>()
                        {
                            //  when the user cancels we need to check where they were so that we can transition smoothly.
                            //  what if they weren't in a dialog? What if they've typed 'cancel' twice in a row?
                            new SendActivity("Really? You want to quit? And we were having so much fun! But have it your way."),
                            new EmitEvent(Constants.Event_Help, bubble: true),
                            new CancelAllDialogs()
                        }
                    },
                    new OnCustomEvent(Constants.Event_Card)
                    {
                        Actions = new List<Dialog>()
                        {
                            new TraceActivity(),
                            new SwitchCondition()
                            {
                                Condition = "toLower(turn.dialogEvent.value)",
                                Cases = new List<Case>()
                                {
                                    //Just copied from below as a quick fix, ideally this would all be in the financing dialog itself.
                                    new Case("financing")
                                    {
                                        Actions = Utilities.CardFactoryActions(profileFactory)
                                    },
                                    new Case("purchasing")
                                    {
                                        Actions = VerifyProfile(nameof(VehicleProfileDialog))
                                    },
                                    new Case("trade-in")
                                    {
                                        Actions = VerifyProfile(nameof(ValueTradeInDialog))
                                    },
                                },
                                Default = new List<Dialog>()
                                {
                                    new SendActivity("I'm sorry, I can't handle that request yet. :("),
                                    new EmitEvent(Constants.Event_Help, bubble: true)
                                }
                            }
                        }
                    },

                    //Every dialog action goes through this
                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
                            new CodeAction(PrimaryHandler),
                        }
                    }
                }
            };

            CardFactories.Add(profileFactory);

            AddDialog(rootDialog);
            AddDialog(financeDialog);
            AddDialog(vehicleDialog);
            AddDialog(tradeInDialog);
            AddDialog(userProfileDialog);
        }

        public IBotServices Services { get; }

        public async Task<DialogTurnResult> PrimaryHandler(DialogContext context, object data)
        {
            //Check if we have an object payload, which comes from cards
            if(context.Context.Activity.Text == null && context.Context.Activity.Value is JObject cardResponse)
            {
                await ProcessCardResponse(cardResponse, context, data);
            }
            //Otherwise if we have a properly instantiated QnA service, hit that.
            else if (Services.LeadQualQnA != null)
            {
                await ProcessDefaultResponse(context, data);
            }
            else
            {
                await context.Context.SendActivityAsync("We must be in Kansas, Dorothy, 'cause there ain't no QnA!");
            }

            //You can change status to alter the behaviour post-completion
            return new DialogTurnResult(DialogTurnStatus.Complete, null);
        }

        //response is json object of card data
        public async Task ProcessCardResponse(JObject response, DialogContext context, object data)
        {
            //look at the card_id field, which has to be assigned on the submit button
            var respondingFactoryID = response.Value<string>("card_id");
            var matchingFactory = CardFactories.SingleOrDefault(cf => cf.Id == respondingFactoryID);

            //If everything checks out validate it, and save if applicable
            if (respondingFactoryID != null && matchingFactory != null)
            {
                if (await matchingFactory.OnValidateCard(response, context.Context))
                {
                    await matchingFactory.OnFinalizeCard(response, context.Context);
                }
                else
                {
                    await context.Context.SendActivityAsync("Looks like you have some errors. You should go back and fix those, and then just resubmit!");
                }
            }
            else
            {
                await context.Context.SendActivityAsync("Not sure where you came from...");
            }
        }

        public async Task ProcessDefaultResponse(DialogContext context, object data)
        {
            //Get Top QnA result
            var results = await Services.LeadQualQnA.GetAnswersAsync(context.Context);
            var topResult = results.FirstOrDefault();
            if (topResult != null)
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
                    await context.EmitEventAsync(Constants.Event_Card, resultTags["card"]);
                }

                //We always send the response from QnA
                await context.Context.SendActivityAsync(MessageFactory.Text(topResult.Answer));
            }
            else
            {
                await context.Context.SendActivityAsync(MessageFactory.Text("Great Caesar's Ghost! " +
                               "You've thrown me for a loop with that one! Give 'er another try, will ya?"));
            }
        }

        public async Task<DialogTurnResult> Test(DialogContext context, object something)
        {
            return new DialogTurnResult(DialogTurnStatus.Complete);
        }


        public List<Dialog> VerifyProfile(string DialogID)
        {
            return new List<Dialog>()
            {
                new IfCondition()
                {
                    Condition = "user.UserProfile == null",
                    Actions = new List<Dialog>()
                    {
                        new SendActivity("Sure! I'd love to help you with that, but I need to collect a few details first."),
                        new BeginDialog(nameof(UserProfileDialog)),
                    },
                    ElseActions = new List<Dialog>()
                    {
                        new SendActivity("Sure! I'd love to help finance you. Let me ask you a few questions about that.")
                    }
                },
                new BeginDialog(DialogID)
            };
        }
    }
}
