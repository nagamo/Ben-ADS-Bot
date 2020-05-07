using ADS.Bot.V1;
using ADS.Bot.V1.Models;
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
                PreInitializeStep,
                InitializeStep,

                CreditScoreStep,
                ValidateCreditScoreStep,

                IncomeStep,
                ValidateIncomeStep,

                HomeOwnershipStep,
                ValidateHomeOwnershipStep,

                EmploymentStep,
                ValidateEmploymentStep,

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
            if (userData?.Financing != null)
            {
                if (userData.Financing.IsCompleted)
                {
                    var resetOptions = Utilities.CreateOptions(new string[] { "Reset", "Use Previous" }, 
                        "Look's like I've already got financing details for you.\r\nWould you to fill those details out again?",
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
                userData.Financing = new FinancingDetails();
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> InitializeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            
            if(stepContext.Result is FoundChoice choice)
            {
                //User was prompted about reseting/continuing
                switch (choice.Value)
                {
                    case "Reset":
                        userData.Financing = new FinancingDetails();
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


        private async Task<DialogTurnResult> CreditScoreStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.Financing.SkipCreditScore) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var creditOptions = Utilities.CreateOptions(new string[] { "<500", "500-600", "600-700", "700+" }, "Hate to ask this, " +
                "but you know it's inevitable, right? Can you please take a swag at your credit score for me?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), creditOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateCreditScoreStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Financing.CreditScore = Utilities.ReadChoiceWithManual(stepContext);

            if (userData.Financing.GoodCredit)
            {
                await stepContext.Context.SendActivityAsync("Wow! Nice score, saved you a bunch of questions too!");
            }

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> IncomeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.Financing.SkipIncome) return await stepContext.NextAsync(cancellationToken: cancellationToken);


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
            if (userData.Financing.SkipHome) return await stepContext.NextAsync(cancellationToken: cancellationToken);



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
            if (userData.Financing.SkipEmployment) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var ownershipOptions = Utilities.CreateOptions(new string[] { "I'm Unemployed", "Less than a year", "1 - 5 years", "More than five years" }, 
                                                           "Last Question! (And you thought we'd never get here!). How long have you been in your current job?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), ownershipOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateEmploymentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Financing.Employment = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> ConfirmAppointmentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (Services.Zoho.Connected)
            {
                var appointmentOptions = Utilities.CreateOptions(new string[] { "Yes!", "No" }, "Would you like to confirm an appointment?");
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
                if (stepContext.Result is FoundChoice appointmentChoice)
                {
                    if (appointmentChoice.Value == "Yes!")
                    {
                        Services.Zoho.CreateUpdateLead(userData);
                        Services.Zoho.WriteFinancingNote(userData);

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
