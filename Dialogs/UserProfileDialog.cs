using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace ADS.Bot1.Dialogs
{
    public class UserProfileDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public UserProfileDialog(UserState userState)
            : base(nameof(UserProfileDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>(nameof(UserProfile));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitStep,
                NameStepAsync,
                NameConfirmStepAsync,
                PhoneStepAsync,
                PhoneConfirmStepAsync,
                InterstsStepAsync,
                FinalizeStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") }, cancellationToken);
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }




        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your phone.") }, cancellationToken);
        }

        private async Task<DialogTurnResult> PhoneConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["phone"] = (string)stepContext.Result;

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> InterstsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["phone"] = (string)stepContext.Result;

            PromptOptions options = new PromptOptions();

            options.Prompt = MessageFactory.Text("What are you intersted in deatils about?");
            options.Choices = new[]
            {
                new Choice("Not sure"),
                new Choice("Financing"),
                new Choice("Trade-In"),
                new Choice("Purchasing"),
            };

            return await stepContext.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var details = new BasicDetails()
            {
                Name = stepContext.Values["name"]?.ToString(),
                Phone = stepContext.Values["phone"]?.ToString()
            };

            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
            userProfile.Details = details;

            await stepContext.Context.SendActivityAsync($"Thank you for confirming your details {details.Name}.");

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}