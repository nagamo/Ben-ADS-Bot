using ADS.Bot.V1.Models;
using ADS.Bot1;
using ADS.Bot1.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Dialogs
{
    public class VehicleProfileDialog : ComponentDialog
    {
        private IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public VehicleProfileDialog(UserState userState)
            : base(nameof(VehicleProfileDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>(nameof(UserProfile));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitializeStep,

                GoalsStep,
                ValidateGoalsStep,

                InterestStep,
                ValidateInterestStep,

                VehicleTypeStep,
                ValidateTypeStep,

                VehicleBrandStep,
                ValidateVehicleBrancStep,

                NewUsedStep,
                ValidateNewUsedStep,

                BudgetStep,
                ValidateBudgetStep,

                NeedFinancingStep,
                ExecuteFinancingStep,

                TradingInStep,
                ExecuteTradingInStep,

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
            if (userData?.VehicleProfile != null)
            {
                if (userData.VehicleProfile.IsCompleted)
                {
                    return await stepContext.EndDialogAsync();
                }
            }
            else
            {
                userData.VehicleProfile = new VehicleInventoryDetails();
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }





        private async Task<DialogTurnResult> GoalsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Goals)) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var goalOptions = Utilities.CreateOptions(new string[] { "Buy", "Lease", "Not Sure" }, "What are you looking to do?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), goalOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateGoalsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Goals = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> InterestStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.LevelOfInterest)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var interestOptions = Utilities.CreateOptions(new string[] { "Ready now", "<30 Days", "30-90 Days", "90+ Days" }, "How soon are you looking to make you purchase?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), interestOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateInterestStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.LevelOfInterest = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> VehicleTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Type)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var typeOptions = Utilities.CreateOptions(new string[] { "SUV", "Sedan", "Pickup", "EV", "Other" }, "What kind of vehicle are you interested in?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), typeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Type = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> VehicleBrandStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Brand)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var brandOptions = Utilities.CreateOptions(new string[] { "Chevrolet", "Toyota", "Honda" }, "What brand are you looking for?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), brandOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateVehicleBrancStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Brand = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> NewUsedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.NewUsed)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var brandOptions = Utilities.CreateOptions(new string[] { "New", "Used", "Certified Pre-Owned" }, "Looking to buy new or used?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), brandOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateNewUsedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.NewUsed = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> BudgetStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Budget)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var brandOptions = Utilities.CreateOptions(new string[] { "< $1000", "$1000-5000", "$5000+" }, "Roughly what is your budget?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), brandOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateBudgetStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Budget = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> NeedFinancingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (userData.VehicleProfile.NeedFinancing != null) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var financeOptions = Utilities.CreateOptions(new string[] { "Yes", "No" }, "Do you need financing?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), financeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ExecuteFinancingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.NeedFinancing = Utilities.ReadChoiceWithManual(stepContext).Equals("yes", StringComparison.OrdinalIgnoreCase);

            if (userData.VehicleProfile.NeedFinancing ?? false)
            {
                return await stepContext.BeginDialogAsync(nameof(FinanceDialog));
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }



        private async Task<DialogTurnResult> TradingInStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (userData.VehicleProfile.TradingIn != null) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var tradeinOptions = Utilities.CreateOptions(new string[] { "Yes", "No" }, "Will you be trading in your current vehicle?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), tradeinOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ExecuteTradingInStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.TradingIn = Utilities.ReadChoiceWithManual(stepContext).Equals("yes", StringComparison.OrdinalIgnoreCase);

            if (userData.VehicleProfile.TradingIn ?? false)
            {
                return await stepContext.BeginDialogAsync(nameof(ValueTradeInDialog));
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }



        private async Task<DialogTurnResult> FinalizeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);

            var lines = new List<string>
            {
                $"Thanks {userData.Name}! I got these details for you.",
                $"I hope it's correct becuase I don't reset yet! :)",
                $"Goals: {userData.VehicleProfile.Goals}",
                $"Urgency: {userData.VehicleProfile.LevelOfInterest}",
                $"Type: {userData.VehicleProfile.Type}",
                $"Brand: {userData.VehicleProfile.Brand}",
                $"New/Used: {userData.VehicleProfile.NewUsed}",
                $"Budget: {userData.VehicleProfile.Budget}"
            };

            lines.Add($"Financing?: {userData.VehicleProfile.NeedFinancing}");
            if (userData.VehicleProfile.NeedFinancing ?? false)
            {
                lines.Add($"Credit Score: {userData.Financing.CreditScore}");
                lines.Add($"Income: {userData.Financing.Income}");
                lines.Add($"Home Ownership: {userData.Financing.HomeOwnership}");
                lines.Add($"Employment History: {userData.Financing.Employment}");
            }

            lines.Add($"Trading In?: {userData.VehicleProfile.TradingIn}");
            if(userData.VehicleProfile.TradingIn ?? false)
            {
                lines.Add($"Make: {userData.TradeDetails.Make}");
                lines.Add($"Model: {userData.TradeDetails.Model}");
                lines.Add($"Year: {userData.TradeDetails.Year}");
                lines.Add($"Condition: {userData.TradeDetails.Condition}");
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Join(Environment.NewLine, lines)), cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
