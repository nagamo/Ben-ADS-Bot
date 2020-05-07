using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ADS.Bot.V1.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.Sequence;

namespace ADS.Bot1.Dialogs
{
    public class UserProfileDialog : ComponentDialog
    {
        const string PROMPT_Name = "NameInput";
        const string PROMPT_Phone = "PhoneInput";
        const string PROMPT_Email = "EmailInput";

        public IBotServices Services { get; }

        public UserProfileDialog(IBotServices services)
            : base(nameof(UserProfileDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitStep,

                NameStepAsync,
                NameConfirmStepAsync,
                
                PhoneStepAsync,
                PhoneConfirmStepAsync,

                EmailStepAsync,
                EmailConfirmStepAsync,

                FinalizeStepAsync
            };


            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(PROMPT_Name, ValidateNameAsync));
            AddDialog(new TextPrompt(PROMPT_Phone, ValidatePhoneAsync));
            AddDialog(new TextPrompt(PROMPT_Email, ValidateEmailAsync));
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




        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if(userData.Details.Phone != null)
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            string line = "";
            if (stepContext.Result != null)
            {
                line = $"Great to meet you, {userData.Name}! Can I get your cell number in case we get disconnected?";
            }
            else
            {
                line = "Real quick, can I get your cell number in case we get disconnected?";
            }

            return await stepContext.PromptAsync(PROMPT_Phone, new PromptOptions {
                Prompt = MessageFactory.Text(line),
                RetryPrompt = MessageFactory.Text("Um, that seems wrong. Try again?")
            }, cancellationToken);
        }

        private async Task<bool> ValidatePhoneAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var phoneDetails = SequenceRecognizer.RecognizePhoneNumber(promptContext.Context.Activity.Text, "en");
            
            return phoneDetails.Count != 0;
        }

        private async Task<DialogTurnResult> PhoneConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if(stepContext.Result != null)
            {
                var phoneDetails = SequenceRecognizer.RecognizePhoneNumber(stepContext.Context.Activity.Text, "en");
                userData.Details.Phone = phoneDetails.First().Text;

                await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);
            }
            
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }




        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (userData.Details.Email != null)
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.PromptAsync(PROMPT_Email, new PromptOptions
            {
                Prompt = MessageFactory.Text("So far, so good, " + userData.Name + "! Now, could we get your email? And seriously, we won't pass it to anyone."),
                RetryPrompt = MessageFactory.Text("Not to be critical, but I think that's invalid. Wanna give it another go?")
            }, cancellationToken);
        }

        private async Task<bool> ValidateEmailAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var emailDetails = SequenceRecognizer.RecognizeEmail(promptContext.Context.Activity.Text, "en");

            return emailDetails.Count != 0;
        }

        private async Task<DialogTurnResult> EmailConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (stepContext.Result != null)
            {
                var emailDetails = SequenceRecognizer.RecognizeEmail(stepContext.Result as string, "en");
                userData.Details.Email = emailDetails.First().Text;

                await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            //Save it back to our storage
            await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);

            await stepContext.Context.SendActivityAsync($"You're the cat's pyjamas, {userData.Details.Name}!");
            await stepContext.Context.SendActivityAsync("And now, without further ado - onto your destination!");

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}