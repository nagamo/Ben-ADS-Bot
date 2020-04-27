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
        public IBotServices Services { get; }



        public ActiveLeadDialog(
            ICardFactory<BasicDetails> profileFactory,
            ICardFactory<FinancingDetails> financeFactory,
            ICardFactory<TradeInDetails> tradeinFactory,
            ICardFactory<VehicleInventoryDetails> vehicleFactory,
            IBotServices botServices) 
            : base(nameof(ActiveLeadDialog))
        {
            Services = botServices;

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(),
                Triggers = new List<OnCondition>()
                {
                    new OnCustomEvent(Constants.Event_Card_Submit)
                    {
                        Actions = new List<Dialog>()
                        {
                            //Update user object with details from profile
                            //I thought this would happen automatically somewhere...
                            new CodeAction(async (context, _)=>{
                                var userData = await Services.GetUserProfileAsync(context.Context);
                                context.GetState().SetValue("user.UserProfile", userData);
                                return new DialogTurnResult(DialogTurnStatus.Complete);
                            }),
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
                                }
                            },
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
                                Property = "conversation.interest",
                                Choices = new ChoiceSet(Constants.HelpOptions.Select(o => new Choice(o)).ToList())
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
                                Condition = "user.UserProfile.Details == null",
                                Actions = new List<Dialog>()
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
                                },
                                ElseActions = new List<Dialog>()
                                {
                                    new SwitchCondition()
                                    {
                                        Condition = "conversation.interest",
                                        Cases = new List<Case>()
                                        {
                                            new Case() {
                                                Value = "Explore Financing",
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'financing'") }
                                            },
                                            new Case() {
                                                Value = "Identify a Vehicle",
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'vehicle'") }
                                            },
                                            new Case() {
                                                Value = "Value a Trade-In",
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'tradein'") }
                                            },
                                            new Case() {
                                                Value = "Search Inventory",
                                                Actions = new List<Dialog>(){ new EmitEvent(Constants.Event_Card, "'inventory'") }
                                            }
                                        }
                                    }
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
                            new SendActivity("Really? You want to quit? And we were having so much fun! But have it your way."),
                            new EmitEvent(Constants.Event_Help, bubble: true),
                            new CancelAllDialogs(),
                        }
                    },
                    new OnCustomEvent(Constants.Event_Card)
                    {
                        Actions = new List<Dialog>()
                        {
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
                                            Utilities.CardFactoryAction(profileFactory)
                                        }
                                    },
                                    new Case("financing")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            Utilities.CardFactoryAction(financeFactory)
                                        }
                                    },
                                    new Case("vehicle")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            Utilities.CardFactoryAction(vehicleFactory)
                                        }
                                    },
                                    new Case("tradein")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            Utilities.CardFactoryAction(tradeinFactory)
                                        }
                                    },
                                    new Case("inventory")
                                    {
                                        Actions = new List<Dialog>()
                                        {
                                            Utilities.CardFactoryAction(vehicleFactory)
                                        }
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

                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
#if DEBUG
                            new TraceActivity(){Name = "OnBeginDialog"},
#endif
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
        }

        public async Task<DialogTurnResult> DispalyHelp(DialogContext context, object data)
        {
            await context.Context.SendActivityAsync(MessageFactory.SuggestedActions(Constants.HelpOptions));
            //await context.Context.SendActivityAsync("Test");

            return new DialogTurnResult(DialogTurnStatus.Waiting, null);
        }

        public async Task<DialogTurnResult> PrimaryHandler(DialogContext context, object data)
        {
            //Check if we have an object payload, which comes from cards
            if(context.Context.Activity.Text == null)
            {
                if (context.Context.Activity.Value is JObject cardResponse)
                {
                    return await ProcessCardResponse(cardResponse, context, data);
                }
                else
                {
                    //Waiting for user to specify something.
                    //Usually come here after showing help screen.
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }
            //Otherwise if we have a properly instantiated QnA service, hit that.
            else if (Services.LeadQualQnA != null)
            {
                if(Constants.HelpOptions.Contains(context.Context.Activity.Text))
                {
                    //await context.EmitEventAsync(Constants.Event_Interest);
                    return new DialogTurnResult(DialogTurnStatus.CompleteAndWait);
                }
                else
                {
                    return await ProcessDefaultResponse(context, data);
                }
            }
            else
            {
                await context.Context.SendActivityAsync("We must be in Kansas, Dorothy, 'cause there ain't no QnA!");
            }

            //You can change status to alter the behaviour post-completion
            return new DialogTurnResult(DialogTurnStatus.Waiting, null);
        }

        //response is json object of card data
        public async Task<DialogTurnResult> ProcessCardResponse(JObject response, DialogContext context, object data)
        {
            //look at the card_id field, which has to be assigned on the submit button
            var respondingFactoryID = response.Value<string>("card_id");
            var matchingFactory = CardFactories.SingleOrDefault(cf => cf.Id == respondingFactoryID);

            switch (response.Value<string>("id"))
            {
                case "submit":
                    //If everything checks out, validate it, and save if applicable
                    if (respondingFactoryID != null && matchingFactory != null)
                    {
                        if (await matchingFactory.OnValidateCard(response, context.Context))
                        {
                            await matchingFactory.OnFinalizeCard(response, context.Context);

                            await context.EmitEventAsync(Constants.Event_Card_Submit);

                            return new DialogTurnResult(DialogTurnStatus.Complete);
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
                    break;
                case "cancel":
                    await context.Context.SendActivityAsync("Canceled card");
                    //If we cancel, we want to emit the help again.
                    await context.EmitEventAsync(Constants.Event_Help);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
                    break;
            }

            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        public async Task<DialogTurnResult> ProcessDefaultResponse(DialogContext context, object data)
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

                    return new DialogTurnResult(DialogTurnStatus.Complete);
                }

                //We always send the response from QnA
                await context.Context.SendActivityAsync(MessageFactory.Text(topResult.Answer));
            }
            else
            {
                await context.Context.SendActivityAsync(MessageFactory.Text("Great Caesar's Ghost! " +
                               "You've thrown me for a loop with that one! Give 'er another try, will ya?"));
            }

            return new DialogTurnResult(DialogTurnStatus.Waiting);
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
