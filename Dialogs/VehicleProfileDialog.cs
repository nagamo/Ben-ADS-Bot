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

        public IBotServices Services { get; }

        public VehicleProfileDialog(UserState userState, IBotServices services)
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
            Services = services;
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
            //if (!string.IsNullOrEmpty(userData.VehicleProfile.Goals)) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var goalOptions = Utilities.CreateOptions(new string[] { "Buy", "Lease", "Not Sure" }, "What are you looking to do?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), goalOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateGoalsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            //if (stepContext.Result != null)
            //    userData.VehicleProfile.Goals = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> InterestStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            //if (!string.IsNullOrEmpty(userData.VehicleProfile.LevelOfInterest)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var interestOptions = Utilities.CreateOptions(new string[] { "Ready now", "<30 Days", "30-90 Days", "90+ Days" }, "How soon are you looking to make you purchase?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), interestOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateInterestStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            //if (stepContext.Result != null)
            //    userData.VehicleProfile.LevelOfInterest = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> VehicleTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Make)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var typeOptions = Utilities.CreateOptions(new string[] { "SUV", "Sedan", "Pickup", "EV", "Other" }, "What kind of vehicle are you interested in?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), typeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Make = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> VehicleBrandStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Model)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var brandOptions = Utilities.CreateOptions(new string[] { "Chevrolet", "Toyota", "Honda" }, "What brand are you looking for?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), brandOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateVehicleBrancStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Model = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> NewUsedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Year)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var brandOptions = Utilities.CreateOptions(new string[] { "New", "Used", "Certified Pre-Owned" }, "Looking to buy new or used?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), brandOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateNewUsedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Year = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> BudgetStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            //if (!string.IsNullOrEmpty(userData.VehicleProfile.Budget)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var brandOptions = Utilities.CreateOptions(new string[] { "< $1000", "$1000-5000", "$5000+" }, "Roughly what is your budget?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), brandOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateBudgetStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            //if (stepContext.Result != null)
            //    userData.VehicleProfile.Budget = Utilities.ReadChoiceWithManual(stepContext);

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
                if (userData.Financing?.IsCompleted ?? false)
                {
                    await stepContext.Context.SendActivityAsync("Looks like I've already got your financing details. So I won't ask you about those again.");
                }
                else
                {
                    return await stepContext.BeginDialogAsync(nameof(FinanceDialog));
                }
                
            }
            
            return await stepContext.NextAsync();
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
                if (userData.TradeDetails?.IsCompleted ?? false)
                {
                    await stepContext.Context.SendActivityAsync("Looks like I've already got your trade-in details. So I won't ask you about those again.");
                }
                else
                {
                    return await stepContext.BeginDialogAsync(nameof(ValueTradeInDialog));
                }
            }
            
            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> FinalizeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);

            var lines = new List<string>
            {
                $"Thanks {userData.Name}!"
            };

            if (Services.Zoho.Connected)
            {
                Services.Zoho.WriteVehicleProfileNote(userData);
            }
            else
            {
                //TODO: What to do if CRM isn't configured properly...
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Join(Environment.NewLine, lines)), cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
