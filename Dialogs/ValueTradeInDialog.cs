using ADS.Bot.V1.Models;
using ADS.Bot.V1.Services;
using ADS.Bot1;
using ADS.Bot1.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Dialogs
{
    public class ValueTradeInDialog : ComponentDialog
    {
        private ADSBotServices Services { get; }

        public ValueTradeInDialog(ADSBotServices services)
            : base(nameof(ValueTradeInDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                PreInitializeStep,
                InitializeStep,

                VehicleStep,
                ValidateVehicleStep,

                ConditionStep,
                ValidateConditionStep,

                AmountOwedStep,
                ValidateAmountOwedStep,

                ConfirmAppointmentStep,
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


        private async Task<DialogTurnResult> PreInitializeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData?.TradeDetails != null)
            {
                if (userData.TradeDetails.IsCompleted)
                {
                    var resetOptions = Utilities.CreateOptions(new string[] { "Reset", "Use Previous" },
                        "Look's like I've already got trade-in details for you.\r\nWould you to fill those details out again?",
                        "Not sure what you meant, try again?");
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), resetOptions, cancellationToken);
                }
                else
                {
                    var resetOptions = Utilities.CreateOptions(new string[] { "Reset", "Resume" },
                        "Look's like you have some details filled in already.\r\nDo you want to pick up where you left off, or fill things out again?",
                        "Not sure what you meant, try again?");
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), resetOptions, cancellationToken);
                }
            }
            else
            {
                userData.TradeDetails = new TradeInDetails();
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> InitializeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (stepContext.Result is FoundChoice choice)
            {
                //User was prompted about reseting/continuing
                switch (choice.Value)
                {
                    case "Reset":
                        userData.TradeDetails = new TradeInDetails();
                        break;
                    case "Resume":
                        //Don't need to do anything, each sub-dialog will skip
                    case "Use Previous":
                        //Let this ripple through all stages, will go to end if everything is already there.
                        break;
                }
            }

            //For every other case, we can just continue in the dialog
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }



        private async Task<DialogTurnResult> VehicleStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.TradeDetails.SkipVehicle) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var options = new PromptOptions()
            {
                Prompt = MessageFactory.Text("Give a brief description of your trade-in vehicle. (eg: 2014 Chevy Cruze, 1995 Honda Civic)")
            };
            return await stepContext.PromptAsync(nameof(TextPrompt), options, cancellationToken: cancellationToken);
        }
        
        private async Task<DialogTurnResult> ValidateVehicleStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.TradeDetails.Vehicle = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> ConditionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.TradeDetails.SkipCondition) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var conditionOptions = Utilities.CreateOptions(new string[] { "Like New", "Great Shape", "Fair", "Rough" }, "What kind of shape is it in?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), conditionOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateConditionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.TradeDetails.Condition = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> AmountOwedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.TradeDetails.SkipAmountOwed) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var owedOptions = Utilities.CreateOptions(new string[] { "It's paid off!", "< $1000", "$1k-5k", "$5k+" }, "How much do you still owe?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), owedOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateAmountOwedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.TradeDetails.AmountOwed = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> ConfirmAppointmentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (Services.CRM.IsActive)
            {
                var appointmentOptions = Utilities.CreateOptions(new string[] { "Yes!", "No" }, "Would you like our appraiser to contact you for a quick valuation of your trade?");
                return await stepContext.PromptAsync(nameof(ChoicePrompt), appointmentOptions, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalizeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (Services.CRM.IsActive)
            {
                if(stepContext.Result is FoundChoice appointmentChoice)
                {
                    userData.Details.RequestContact = appointmentChoice.Value == "Yes!";
                }

                Services.CRM.WriteCRMDetails(CRMStage.ValueTradeInCompleted, userData);

                if (userData.Details.RequestContact)
                {
                    await stepContext.Context.SendActivityAsync("Thanks! Someone will be in touch with you shortly.");
                }
                else {
                    await stepContext.Context.SendActivityAsync("Thanks for filling that out, I'll remember your details in case you want to come back and make an appointment later.");
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Thanks for filling that out!");
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
