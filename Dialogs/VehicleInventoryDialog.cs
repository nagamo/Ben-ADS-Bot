using ADS.Bot.V1;
using ADS.Bot.V1.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot1.Dialogs
{
    public class VehicleInventoryDialog : ComponentDialog
    {
        private IBotServices Services { get; }

        public VehicleInventoryDialog(IBotServices services)
            : base(nameof(VehicleInventoryDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                PreInitializeStep,
                InitializeStep,

                PrimaryConcernStep,
                ValidatePrimaryConcernStep,

                ConcernGoalStep,
                ValidateConcernStep,

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
            //Select as little data as possible
            var foundCars = Services.CarStorage.ExecuteQuery(new TableQuery<VS_Car>() {SelectColumns = new string[] { "RowKey" } });
            await stepContext.Context.SendActivityAsync($"I'm glad you asked aobut my inventory. I just so happen to have {foundCars.Count():n0} cars available!");

            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData?.Inventory != null)
            {
                if (userData.Inventory.IsCompleted)
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
                userData.Inventory = new VehicleInventoryDetails();
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
                        userData.Inventory = new VehicleInventoryDetails();
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





        private async Task<DialogTurnResult> PrimaryConcernStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.Inventory.SkipPrimaryConcern) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var concernOptions = Utilities.CreateOptions(new string[] { "Overall Price", "Monthly Payment", "Make", "Color", "Nothing Specific" }, "What is your primary concern regarding a vehicle puchase?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), concernOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidatePrimaryConcernStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Inventory.PrimaryConcern = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> ConcernGoalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.Inventory.SkipConcernGoal) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            PromptOptions goalOptions = null;

            switch (userData.Inventory.PrimaryConcern)
            {
                case "Overall Price":
                    goalOptions = Utilities.CreateOptions(new string[] { "<$1000", "$1000-5000", "$5000-10k", "$10k+" }, "How much are you looking to spend?");
                    break;
                case "Monthly Payment":
                    goalOptions = Utilities.CreateOptions(new string[] { "<$100/month", "$100-200/month", "$200-400/month", "$400+/month" }, "What are you aiming for a monthly payment?");
                    break;
                case "Make":
                    goalOptions = Utilities.CreateOptions(new string[] { "Chevrolet", "Honda", "Toyota", "Ford" }, "What make of car are you interested in?");
                    break;
                case "Color":
                    goalOptions = Utilities.CreateOptions(new string[] { "Red", "Blue", "Green", "Black", "Silver" }, "What are you aiming for a monthly payment?");
                    break;
                case "Nothing Specific":
                    goalOptions = Utilities.CreateOptions(new string[] { "Yes!", "Not Exactly..", "Just looking" }, "Do you know what kind of vehicle you want?");
                    break;
            }

            if (goalOptions != null)
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt), goalOptions, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions() {Prompt = MessageFactory.Text("How can we help with that concern?") }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ValidateConcernStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.Inventory.ConcernGoal = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> ConfirmAppointmentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            IQueryable<VS_Car> carQuery = Services.CarStorage.CreateQuery<VS_Car>();
            (carQuery as TableQuery<VS_Car>).SelectColumns = new string[] { "RowKey" };


            if (userData.Inventory.PrimaryConcern != "Nothing Specific")
            {
                switch (userData.Inventory.PrimaryConcern)
                {
                    //case "Overall Price":
                    //carQuery.Where(c => c.Price < )
                    //    break;
                    //case "Monthly Payment":
                    //    break;
                    case "Make":
                        carQuery = carQuery.Where(c => c.Make.Equals(userData.Inventory.ConcernGoal, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "Color":
                        carQuery = carQuery.Where(c => c.Color.Equals(userData.Inventory.ConcernGoal, StringComparison.OrdinalIgnoreCase));
                        break;
                    default:
                        carQuery = null;
                        break;
                }

                if (carQuery != null)
                {
                    var results = Services.CarStorage.ExecuteQuery<VS_Car>(carQuery as TableQuery<VS_Car>);
                    var resultsCount = results.Count();

                    if (resultsCount >= 100)
                    {
                        //100+
                        await stepContext.Context.SendActivityAsync($"Great news {userData.FirstName}! I've actually got {resultsCount:n0} cars that match that {userData.Inventory.PrimaryConcern.ToLower()}!");
                    }
                    else if (resultsCount >= 10)
                    {
                        //10-100
                        await stepContext.Context.SendActivityAsync($"I was able to find {resultsCount:n0} cars for that {userData.Inventory.PrimaryConcern.ToLower()} {userData.FirstName}.");
                    }
                    else if (resultsCount >= 1)
                    {
                        //1-10
                        await stepContext.Context.SendActivityAsync($"I found {resultsCount:n0} cars that match that {userData.Inventory.PrimaryConcern.ToLower()}\r\nI know its not a lot, but we've got plenty of other vehilcles available!");
                    }
                    else
                    {
                        //0
                        await stepContext.Context.SendActivityAsync($"I'm sorry, I dont actually seem to have any cars that match that {userData.Inventory.PrimaryConcern.ToLower()}.\r\nWe'd still love to get in touch to explore what vehicles we have to offer you.");
                    }
                }
            }

            if (Services.Zoho.Connected)
            {
                var appointmentOptions = Utilities.CreateOptions(new string[] { "Yes!", "No" }, "Would you like a call from the GM to further narrow down on a vehicle?");
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
                        Services.Zoho.WriteInventoryNote(userData);

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
