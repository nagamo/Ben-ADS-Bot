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
        public IBotServices Services { get; }

        public FinanceDialog(IBotServices services)
            : base(nameof(FinanceDialog))
        {
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
            Services = services;
        }


        private async Task<DialogTurnResult> InitializeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
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
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (!string.IsNullOrEmpty(userData.Financing.CreditScore)) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var creditOptions = Utilities.CreateOptions(new string[] { "<500", "500-600", "600-700", "700+" }, "Hate to ask this, " +
                "but you know it's inevitable, right? Can you please take a swag at your credit score for me?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), creditOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateCreditScoreStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Financing.CreditScore = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> IncomeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (!string.IsNullOrEmpty(userData.Financing.Income)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var incomeOptions = Utilities.CreateOptions(new string[] { "$1K-$2K per month", "$2K-$4K per month", "$4k - $8K per month", "$8K - $10K per month", "> $10K per month" }, 
                                                        "OK, no fibbing now. Can you provide some sense of your gross monthly income? We promise " +
                                                        "not to say anything to the IRS!");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), incomeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateIncomeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Financing.Income = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> HomeOwnershipStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (!string.IsNullOrEmpty(userData.Financing.HomeOwnership)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var ownershipOptions = Utilities.CreateOptions(new string[] { "Own", "Rent", "Other" }, "Whew! Almost done! How about a little home-ownership information?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), ownershipOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateHomeOwnershipStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Financing.HomeOwnership = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> EmploymentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (!string.IsNullOrEmpty(userData.Financing.Employment)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var ownershipOptions = Utilities.CreateOptions(new string[] { "Unemployed", "Less than a year", "1 - 5 years," +
                                                          "More than five years" }, "Last Question! (I know - you thought we'd " +
                                                           "never get here!). That great job you've got; how long have you been there?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), ownershipOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateEmploymentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Financing.Employment = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> FinalizeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            string details = string.Join(Environment.NewLine, new string[]
            {
                $"Wow {userData.Name}! Thanks! That wasn't so bad, was it? Here's what I picked up,",
                $"and I sure hope we've got it nailed, 'cause we don't want to do THAT again, right?:)",
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
