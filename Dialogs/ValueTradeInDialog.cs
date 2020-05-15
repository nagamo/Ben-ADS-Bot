using ADS.Bot.V1.Models;
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

                MakeStep,
                ValidateMakeStep,

                ModelStep,
                ValidateModelStep,

                YearStep,
                ValidateYearStep,

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





        private async Task<DialogTurnResult> MakeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.TradeDetails.SkipMake) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var makeOptions = Utilities.CreateOptions(new string[] { "Chevrolet", "Toyota", "Honda" }, "What is the make of your trade-in?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), makeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateMakeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.TradeDetails.Make = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> ModelStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.TradeDetails.SkipModel) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var modelOptions = Utilities.CreateOptions(new string[] { "Tacoma", "Tundra", "RAV4" }, "And the Model?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), modelOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateModelStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.TradeDetails.Model = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> YearStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.TradeDetails.SkipYear) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var yearOptions = Utilities.CreateOptions(new string[] { "2019", "2018", "2017", "2016", "2015", "2014", "2013", "2012" }, "What year vehicle is it?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), yearOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateYearStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.TradeDetails.Year = Utilities.ReadChoiceWithManual(stepContext);

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

            if (Services.Zoho.Connected)
            {
                var appointmentOptions = Utilities.CreateOptions(new string[] { "Yes!", "No" }, "Would you like to confirm an appointment for a quick appraisal?");
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

            if (Services.Zoho.Connected)
            {
                if(stepContext.Result is FoundChoice appointmentChoice)
                {
                    if(appointmentChoice.Value == "Yes!")
                    {
                        Services.Zoho.CreateUpdateLead(userData);
                        Services.Zoho.WriteTradeInNote(userData);

                        await stepContext.Context.SendActivityAsync("Thanks! Someone will be in touch with you shortly.");
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                }

                await stepContext.Context.SendActivityAsync("Thanks for filling that out, I'll remember your details in case you want to come back and make an appointment later.");
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            await stepContext.Context.SendActivityAsync("Thanks for filling that out!");
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
