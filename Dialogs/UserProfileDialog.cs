using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ADS.Bot.V1;
using ADS.Bot.V1.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.Sequence;

namespace ADS.Bot1.Dialogs
{
    using Constants = ADS.Bot.V1.Constants;

    public class UserProfileDialog : ComponentDialog
    {
        const string PROMPT_Name = "NameInput";
        const string PROMPT_Contact = "Contact";
        const string PROMPT_Focus = "Focus";
        const string PROMPT_Timeframe = "Timeframe";

        public ADSBotServices Services { get; }

        public UserProfileDialog(ADSBotServices services)
            : base(nameof(UserProfileDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitStep,

                NameStepAsync,
                NameConfirmStepAsync,
                
                ContactStepAsync,
                ContactConfirmStepAsync,

                TimeframeStepAsync,
                TimeframeConfirmStepAsync,

                FinalizeStepAsync
            };


            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(PROMPT_Name, ValidateNameAsync));
            AddDialog(new TextPrompt(PROMPT_Contact, ValidateContactAsync));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
            Services = services;
        }

        private async Task<DialogTurnResult> InitStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            
            if(userData.Details == null)
            {
                userData.Details = new BasicDetails();
            }

            if (!string.IsNullOrWhiteSpace(stepContext.Context.Activity.From.Name))
            {
                /*
                if (stepContext.Context.Activity.From.Name == "Ben Ab")
                {
                    userData.Details = new BasicDetails()
                    {
                        Name = "Ben",
                        Phone = "1231231",
                        Focus = "Focused",
                        Timeframe = "Right now!",
                        Email = "t"
                    };
                    userData.Inventory = new VehicleInventoryDetails()
                    {
                        PrimaryConcern = "Make",
                        ConcernGoal = "Toyota"
                    };
                }
                else
                */
                {
                    //Use their name if they have one supplied.
                    userData.Details.Name = stepContext.Context.Activity.From.Name;
                }
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (userData.Details?.Name != null)
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.PromptAsync(PROMPT_Name, new PromptOptions
            {
                Prompt = MessageFactory.Text("So, first of all - I can't keep saying 'hey you'! Can I get your name, please?"),
                RetryPrompt = MessageFactory.Text("Seriously? I may be a bot, but I'm pretty sure that's not a name! Give it another go, will ya?")
            }, cancellationToken);
        }

        private async Task<bool> ValidateNameAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var nameMatch = Regex.Match(promptContext.Context.Activity.Text, Services.Configuration["name_regex"]);

            return nameMatch.Success;
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            //Skip if we have a null result (name already filled out)
            if(stepContext.Result != null)
            {
                userData.Details.Name = (string)stepContext.Result;

                await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);
            }

            //pass forward reponse for greeting logic specifically
            return await stepContext.NextAsync(stepContext.Result, cancellationToken: cancellationToken);
        }




        private async Task<DialogTurnResult> ContactStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            
            if(userData.Details.Phone != null && userData.Details.Email != null)
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            string line = "";

            if (userData.Details.Phone != null)
            {
                line = "Can I get your email just in case we need to get in touch?";
            }
            else if (userData.Details.Email != null)
            {
                line = "Can I get your cell number in case one of our representatives needs to give you a call?";
            }
            else
            {
                line = "Can I get your email or cell number in case we get disconnected?";
            }

            if (stepContext.Result != null)
            {
                //Result is non-null when the user manuall entered their name
                //So we greet them specifically here
                line = $"Great to meet you, {userData.Name}! {line}";
            }

            return await stepContext.PromptAsync(PROMPT_Contact, new PromptOptions {
                Prompt = MessageFactory.Text(line)
            }, cancellationToken);
        }

        private async Task<bool> ValidateContactAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(promptContext.Context, cancellationToken);

            var pendingEvent = Utilities.CheckEvent(promptContext.Context);
            if(pendingEvent == Constants.Event_Reject)
            {
                var canSkip = userData.Details.Phone != null || userData.Details.Email != null;
                if (!canSkip)
                {
                    await promptContext.Context.SendActivityAsync("I'm sorry, but I need some contact information to go forward.\r\nPlease provide an email or phone numebr.");
                }
                return canSkip;
            }

            var phoneDetails = SequenceRecognizer.RecognizePhoneNumber(promptContext.Context.Activity.Text, "en");
            var emailDetails = SequenceRecognizer.RecognizeEmail(promptContext.Context.Activity.Text, "en");

            return phoneDetails.Count != 0 || emailDetails.Count != 0;
        }

        private async Task<DialogTurnResult> ContactConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            var currentEvent = Utilities.CheckEvent(stepContext.Context);

            //Ignore whatever the response was if the user rejected input
            //TODO: Make this a setting per-dealer?
            if(currentEvent != Constants.Event_Reject)
            {
                if (stepContext.Result != null)
                {
                    var phoneDetails = SequenceRecognizer.RecognizePhoneNumber(stepContext.Context.Activity.Text, "en");
                    var emailDetails = SequenceRecognizer.RecognizeEmail(stepContext.Context.Activity.Text, "en");

                    if (phoneDetails.Count > 0) userData.Details.Phone = phoneDetails.First().Text;
                    if (emailDetails.Count > 0) userData.Details.Email = emailDetails.First().Text;

                    await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);
                }
            }
            
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }



        private async Task<DialogTurnResult> TimeframeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (!string.IsNullOrEmpty(userData.Details.Timeframe)) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            PromptOptions interestOptions = Utilities.CreateOptions(new string[] { "Ready now", "<30 Days", "30-90 Days", "90+ Days" }, "How soon are you looking to make your purchase?");

            return await stepContext.PromptAsync(nameof(ChoicePrompt), interestOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> TimeframeConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            //Skip if we have a null result (name already filled out)
            if (stepContext.Result != null)
            {
                userData.Details.Timeframe = Utilities.ReadChoiceWithManual(stepContext);

                await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);
            }

            //pass forward reponse for greeting logic specifically
            return await stepContext.NextAsync(stepContext.Result, cancellationToken: cancellationToken);
        }






        private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            //Save it back to our storage
            await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);

            if (Services.Zoho.Connected)
            {
                Services.Zoho.CreateUpdateLead(userData);
            }

            await stepContext.Context.SendActivityAsync($"You're the cat's pyjamas, {userData.Details.Name}!");
            await stepContext.Context.SendActivityAsync("And now, without further ado - onto your destination!");

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}