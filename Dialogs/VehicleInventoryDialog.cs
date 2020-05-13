using ADS.Bot.V1;
using ADS.Bot.V1.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot1.Dialogs
{
    public class VehicleInventoryDialog : ComponentDialog
    {
        private IBotServices Services { get; }

        const string InventoryChoice = "INVENTORY";


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
            AddDialog(new ChoicePrompt(InventoryChoice)
            {
                Style = ListStyle.None
            });
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
            Services = services;
        }


        private async Task<DialogTurnResult> PreInitializeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Select as little data as possible
            var foundCars = Services.CarStorage.ExecuteQuery(new TableQuery<DB_Car>() {SelectColumns = new string[] { "RowKey" } });
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
            IQueryable<DB_Car> carQuery = Services.CarStorage.CreateQuery<DB_Car>();
            //(carQuery as TableQuery<VS_Car>).SelectColumns = new string[] { "RowKey" };


            if (userData.Inventory.PrimaryConcern != "Nothing Specific")
            {
                switch (userData.Inventory.PrimaryConcern)
                {
                    //Quick and dirty parsing of premade and limited user-supplied prices
                    case "Overall Price":
                        var enteredPrices = Regex.Matches(userData.Inventory.ConcernGoal, @"([<]*\$?[<]*(\d+)[kK\+]*)");

                        int? minPrice = null, maxPrice = null;

                        if (enteredPrices.Count == 1)
                        {
                            var priceFilter = enteredPrices.First();
                            var thousands = priceFilter.Value.Contains("k", StringComparison.OrdinalIgnoreCase);
                            if (priceFilter.Value.Contains("<")) { maxPrice = int.Parse(priceFilter.Groups[2].Value) * (thousands ? 1000 : 1); }
                            else if (priceFilter.Value.Contains("+")) { minPrice = int.Parse(priceFilter.Groups[2].Value) * (thousands ? 1000 : 1); }
                        }
                        else if (enteredPrices.Count >= 2)
                        {
                            //Bit ugly, but it works :)
                            minPrice = int.Parse(enteredPrices[0].Groups[2].Value) * (enteredPrices[0].Value.Contains("k", StringComparison.OrdinalIgnoreCase) ? 1000 : 1);
                            maxPrice = int.Parse(enteredPrices[1].Groups[2].Value) * (enteredPrices[1].Value.Contains("k", StringComparison.OrdinalIgnoreCase) ? 1000 : 1);
                        }

                        if (minPrice.HasValue && maxPrice.HasValue)
                        {
                            carQuery = carQuery.Where(c => c.Price >= minPrice.Value && c.Price <= maxPrice.Value);
                        }
                        else if (minPrice.HasValue)
                        {
                            carQuery = carQuery.Where(c => c.Price >= minPrice.Value);
                        }
                        else if (maxPrice.HasValue)
                        {
                            carQuery = carQuery.Where(c => c.Price <= maxPrice.Value);
                        }
                        else
                        {
                            carQuery = null;
                        }

                        break;
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
                    var results = Services.CarStorage.ExecuteQuery<DB_Car>(carQuery as TableQuery<DB_Car>);
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

                    if(resultsCount > 0)
                    {
                        var trimmedResults = results.Take(10);

                        var carAttachments = trimmedResults.Select((car_result) => {
                            var card = new HeroCard()
                            {
                                Title = $"{car_result.Year} {car_result.Make} {car_result.Model}",
                                //Text = $"{car_result.Price:C2}",
                                //Subtitle = $"Even more text can go here!",
                                Images = new List<CardImage>()
                                {
                                    new CardImage(car_result.ImageURL)//, tap: new CardAction(ActionTypes.ShowImage, value: car_result.ImageURL))
                                },
                                //Tap = new CardAction("postBack", "I like this one!", value: car_result.VIN),
                                Buttons = new List<CardAction>()
                                {
                                    //new CardAction(ActionTypes.OpenUrl, "Show Details", text: "Open URL", value: car_result.URL),
                                    new CardAction(ActionTypes.ImBack, title: "Im Back", value: car_result.VIN),
                                    //new CardAction(ActionTypes.PostBack, "Post Back", text: "Post Back", value: car_result.VIN)
                                }
                            };

                            return card;
                        });

                        var otherTest = Utilities.CreateTestCarousel(stepContext.Context);
                        await stepContext.Context.SendActivityAsync(otherTest, cancellationToken: cancellationToken);

                        var CARouselActivity = Utilities.CreateCarousel(carAttachments);

                        await stepContext.Context.SendActivityAsync(CARouselActivity);

                        var carOptions = Utilities.CreateOptions(trimmedResults.Select(c => c.VIN), "See any card you like?");
                        return await stepContext.PromptAsync(InventoryChoice, carOptions, cancellationToken: cancellationToken);
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
