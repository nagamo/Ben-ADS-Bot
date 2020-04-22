using ADS.Bot.V1;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot1.Dialogs
{
    public class FinanceDialog : ComponentDialog
    {
        private IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public FinanceDialog(UserState userState)
            : base(nameof(FinanceDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>(nameof(UserProfile));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitializeStep,

                CreditScoreStep,
                ValidateCreditScoreStep,

                IncomeStep,
                ValidateIncomeStep,

                HomeOwnershipStep,
                ValidateHomeOwnershipStep,

                EmploymentStep,
                ValidateEmploymentStep,

                FinalizeStep
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> InitializeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (userData?.Financing != null)
            {
                if (userData.Financing.IsCompleted)
                {
                    return await stepContext.EndDialogAsync();
                }
            }
            else
            {
                userData.Financing = new FinancingDetails();
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> CreditScoreStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.Financing.CreditScore)) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var creditOptions = Utilities.CreateOptions(new string[] { "<500", "500-600", "600-700", "700+" }, "What is your credit score?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), creditOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateCreditScoreStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if(stepContext.Result != null)
                userData.Financing.CreditScore = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> IncomeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.Financing.Income)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var incomeOptions = Utilities.CreateOptions(new string[] { "$1-2k /month", "$2-4k /month", "$4k+ /month" }, "Roughly what is your income?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), incomeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateIncomeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.Financing.Income = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> HomeOwnershipStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.Financing.HomeOwnership)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var ownershipOptions = Utilities.CreateOptions(new string[] { "Own", "Rent", "Other" }, "Do you Own or Rent?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), ownershipOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateHomeOwnershipStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.Financing.HomeOwnership = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> EmploymentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.Financing.Employment)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var ownershipOptions = Utilities.CreateOptions(new string[] { "< 1 yr", "1-5 yrs", "5+ yrs" }, "How long have you been employed?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), ownershipOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateEmploymentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.Financing.Employment = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> FinalizeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);

            string details = string.Join(Environment.NewLine, new string[]
            {
                $"Thanks {userData.Name}! I got these details for you.",
                $"I hope it's correct becuase I don't reset yet! :)",
                $"Credit Score: {userData.Financing.CreditScore}",
                $"Income: {userData.Financing.Income}",
                $"Home Ownership: {userData.Financing.HomeOwnership}",
                $"Employment History: {userData.Financing.Employment}",
            });

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(details), cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
