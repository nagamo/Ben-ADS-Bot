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

namespace ADS.Bot.V1.Dialogs
{
    public class ActiveLeadDialog : ComponentDialog
    {
        public ActiveLeadDialog(VehicleProfileDialog vehicleDialog, ValueTradeInDialog tradeInDialog, FinanceDialog financeDialog, UserProfileDialog userProfileDialog, IBotServices botServices) 
            : base(nameof(ActiveLeadDialog))
        {
            Services = botServices;

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(),
                Recognizer = botServices.Dispatch,
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
                            new LogAction("Options!")
                            {
                                TraceActivity = true
                            },

                            new SwitchCondition()
                            {
                                Condition = "turn.interest",
                                Cases = new List<Case>()
                                {
                                    //Just copied from below as a quick fix, ideally this would all be in the financing dialog itself.
                                    new Case("Financing")
                                    {
                                        Actions = new List<Dialog>()
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
                                            new BeginDialog(nameof(FinanceDialog))
                                        }
                                    },

                                },
                                Default = new List<Dialog>()
                                {
                                    new SendActivity("I'm sorry, I can't handle that request yet. :("),
                                    new EmitEvent(Constants.Event_ShowTips, bubble: true)
                                }

                            },
                            //Delete the property so we don't loop forever
                            new DeleteProperty()
                            {
                                Property = "turn.interest"
                            }
                        }
                    },
                    new OnIntent("Financing")
                    {
                        // again, are they coming back, or is this their first visit?
                        //*From Ben*
                        //  The way I currently implemented it:
                        //          FinanceDialog itself will essentially skip all the way to the end internally and just print the summary.
                        //  Below is a more explicit implementation using Condition, which has a few shorthands to it (buried in the repositories)
                        //  $ works with dialog-scope variables (eg. $userName = dialog.userName), these only last as long as the dialog
                        //  # works with LUIS intents (eg. Below, #Financing.Score >= 0.8 is looking at the luis response data)
                        //  you can access needed data with variables like user, conversation, turn, etc. all automatically mapped to classes like UserState, ConversationState, etc.
                        Condition = "#Financing.Score >= 0.8 && user.UserProfile != null",
                        Actions = new List<Dialog>()
                        {
                            //*From Ben*
                            //  In this case, we handle them not having a profile, and show them the user profile dialog first.
                            //  I understand this isn't 100% how you want it implemented, but should be a lot closer.
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
                            new BeginDialog(nameof(FinanceDialog))
                        }
                    },
                    new OnIntent("Purchasing")
                    {
                        Condition = "#Purchasing.Score >= 0.8",
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
                            //  this should display a card that gives them some explanations and a set of discrete selections.
                            //  Need to bubble it so it goes up to RootDialog
                            new EmitEvent(Constants.Event_ShowTips, bubble: true),
                            new CancelAllDialogs()
                        }
                    },
                    new OnIntent("Cancel")
                    {
                        Condition = "#Cancel.Score >= 0.8",
                        Actions = new List<Dialog>()
                        {
                            //  when the user cancels we need to check where they were so that we can transition smoothly.
                            //  what if they weren't in a dialog? What if they've typed 'cancel' twice in a row?
                            new SendActivity("Really? You want to quit? And we were having so much fun! But have it your way."),
                            new EmitEvent(Constants.Event_ShowTips, bubble: true),
                            new CancelAllDialogs()
                        }
                    },
                    new OnUnknownIntent()
                    {
 
            Actions = new List<Dialog>()
                        {
                            new CodeAction(QNAFallback),
                            //new SendActivity("OK, you totally lost me! Give it another shot, will ya??")
                        }
                    }
                }
            };

            AddDialog(rootDialog);
            AddDialog(financeDialog);
            AddDialog(vehicleDialog);
            AddDialog(tradeInDialog);
            AddDialog(userProfileDialog);
        }

        public IBotServices Services { get; }

        public async Task<DialogTurnResult> QNAFallback(DialogContext context, object something)
        {
            if (Services.LeadQualQnA != null)
            {
                var results = await Services.LeadQualQnA.GetAnswersAsync(context.Context);
                if (results.Any())
                {
                    await context.Context.SendActivityAsync(MessageFactory.Text(results.First().Answer));
                }
                else
                {
                    await context.Context.SendActivityAsync(MessageFactory.Text("Great Caesar's Ghost! " +
                                   "You've thrown me for a loop with that one! Give 'er another try, will ya?"));
                }
            }
            else
            {
                await context.Context.SendActivityAsync("We must be in Kansas, Dorothy, 'cause there ain't no QnA!");
            }

            //You can change status to alter the behaviour post-completion
            return new DialogTurnResult(DialogTurnStatus.Complete, null);
        }

        public async Task<DialogTurnResult> Test(DialogContext context, object something)
        {
            return null;
        }

    }
}
