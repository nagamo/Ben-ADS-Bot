using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            AddDialog(new TextPrompt(nameof(TextPrompt)));
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
            userData.Details = new BasicDetails();

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") }, cancellationToken);
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            userData.Details.Name = (string)stepContext.Result;

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }




        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(PROMPT_Phone, new PromptOptions {
                Prompt = MessageFactory.Text("Please enter your phone."),
                RetryPrompt = MessageFactory.Text("I'm sorry, that doesn't appear to be a valid phone number.")
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
            userData.Details.Phone = (string)stepContext.Result;

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }




        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(PROMPT_Email, new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter your email."),
                RetryPrompt = MessageFactory.Text("I'm sorry, that doesn't appear to be a valid email address.")
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
            userData.Details.Email = (string)stepContext.Result;

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            await stepContext.Context.SendActivityAsync($"Thank you for confirming your details {userData.Details.Name}.");

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}