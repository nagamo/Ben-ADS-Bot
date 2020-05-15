using ADS.Bot.V1;
using ADS.Bot.V1.Models;
using ADS.Bot.V1.Services;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private ADSBotServices Services { get; }

        const string InventoryChoice = "INVENTORY";


        public VehicleInventoryDialog(ADSBotServices services)
            : base(nameof(VehicleInventoryDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                PreInitializeStep,
                InitializeStep,

                NewUsedStepAsync,
                NewUsedConfirmStepAsync,

                PrimaryConcernStep,
                ValidatePrimaryConcernStep,

                ConcernGoalStep,
                ValidateConcernStep,

                ShowInventoryStep,

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

        private TableQuery<DB_Car> BuildQuery(UserProfile UserData)
        {
            if(UserData.Inventory.PrimaryConcern == "Nothing Specific")
                return null;

            IQueryable<DB_Car> carQuery = Services.CarStorage.CreateQuery<DB_Car>();

            switch (UserData.Inventory.NewUsed)
            {
                case "New":
                    carQuery = carQuery.Where(car => car.Used == false);
                    break;
                case "Used":
                    carQuery = carQuery.Where(car => car.Used == true);
                    break;
            }

            if (!string.IsNullOrEmpty(UserData.Inventory.ConcernGoal))
            {
                switch (UserData.Inventory.PrimaryConcern)
                {
                    //Quick and dirty parsing of premade and limited user-supplied prices
                    case "Price":
                        var enteredPrices = Regex.Matches(UserData.Inventory.ConcernGoal, @"([<]*\$?[<]*(\d+)[kK\+]*)");

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
                        carQuery = carQuery.Where(c => c.Make.Equals(UserData.Inventory.ConcernGoal, StringComparison.OrdinalIgnoreCase));
                        break;
                    default:
                        carQuery = null;
                        break;
                }
            }

            return carQuery as TableQuery<DB_Car>;
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



        private async Task<DialogTurnResult> NewUsedStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (!string.IsNullOrEmpty(userData.Inventory.NewUsed)) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            var goalOptions = Utilities.CreateOptions(new string[] { "Doesn't Matter", "New", "Used" }, "Are you shopping new or used?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), goalOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> NewUsedConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (stepContext.Result != null)
            {
                userData.Inventory.NewUsed = Utilities.ReadChoiceWithManual(stepContext);

                await Services.SetUserProfileAsync(userData, stepContext, cancellationToken);

                var newUsedQuery = BuildQuery(userData);

                if(newUsedQuery != null)
                {
                    var foundCars = Services.CarStorage.ExecuteQuery(newUsedQuery).ToList();

                    if (userData.Inventory.NewUsed != "Doesn't Matter")
                    {
                        await stepContext.Context.SendActivityAsync($"I've got {foundCars.Count()} {userData.Inventory.NewUsed?.ToLower()} cars.");
                    }
                }
            }

            //pass forward reponse for greeting logic specifically
            return await stepContext.NextAsync(stepContext.Result, cancellationToken: cancellationToken);
        }





        private async Task<DialogTurnResult> PrimaryConcernStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.Inventory.SkipPrimaryConcern) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var concernOptions = Utilities.CreateOptions(new string[] { "Price", "Make", "Nothing Specific" }, "What is your primary concern regarding a vehicle puchase?");
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
                case "Price":
                    goalOptions = Utilities.CreateOptions(new string[] { "<$10k", "$10k-20k", "$20k-30k", "$30k-40k", "$40k+" }, "How much are you looking to spend?");
                    break;
                case "Make":
                    var makeOptions = BuyerBridgeService
                        .ListAvailableMakes(Services, BuildQuery(userData))
                        .OrderByDescending(mo => mo.Count)
                        .Select(mo => $"{mo.Make} ({mo.Count})")
                        .Take(10);

                    goalOptions = Utilities.CreateOptions(makeOptions, "What make of car are you interested in?");
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
            {
                userData.Inventory.ConcernGoal = Utilities.ReadChoiceWithManual(stepContext);

                if(userData.Inventory.PrimaryConcern == "Make")
                {
                    //This is to turn "Make (Count)" into just "Make"
                    userData.Inventory.ConcernGoal = userData.Inventory.ConcernGoal.Split(" (").First();
                }
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> ShowInventoryStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);


            var carQuery = BuildQuery(userData);

            if (carQuery != null)
            {
                var results = Services.CarStorage.ExecuteQuery<DB_Car>(carQuery).ToList();
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

                if (resultsCount > 0)
                {
                    var trimmedResults = results.Take(5);

                    await stepContext.Context.SendActivityAsync($"Here are the top {trimmedResults.Count()} cars I was able to find for you.");


                    var carAttachments = trimmedResults.Select((car_result) => {
                        var card = new HeroCard()
                        {
                            Title = $"{car_result.Display_Name}",
                            Text = $"{car_result.Price:C2}",
                            Images = new List<CardImage>()
                            {
                                new CardImage()
                                {
                                    Url = car_result.Image_URL,
                                    Tap = new CardAction()
                                    {
                                        Type = ActionTypes.ShowImage,
                                        Value = car_result.Image_URL
                                    }
                                }
                            },
                            Buttons = new List<CardAction>()
                            {
                                new CardAction()
                                {
                                    Type = ActionTypes.OpenUrl,
                                    Title = "See Details Online",
                                    Value = car_result.URL
                                },
                                new CardAction()
                                {
                                    Type = ActionTypes.PostBack,
                                    Title = "I like this one!",
                                    DisplayText = $"I like #{car_result.VIN}",
                                    Text = $"I like #{car_result.VIN}",
                                    Value = car_result.VIN
                                }
                            }
                        };

                        if (car_result.Used)
                        {
                            card.Subtitle = $"Used: {car_result.Mileage:D0} Miles";
                        }
                        else
                        {
                            card.Subtitle = $"New. {car_result.Engine}, {car_result.Doors} Door";
                        }

                        return card.ToAttachment();
                    });

                    var CARouselActivity = Utilities.CreateCarousel(carAttachments);

                    //Supply the options here so the prompt code can line up out postback values to our list of VINs
                    var carOptions = Utilities.CreateOptions(trimmedResults.Select(c => c.VIN), CARouselActivity as Activity);
                    return await stepContext.PromptAsync(InventoryChoice, carOptions, cancellationToken: cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync($"I'm sorry, I dont actually seem to have any cars that match that {userData.Inventory.PrimaryConcern.ToLower()}.\r\nWe'd still love to get in touch to explore what vehicles we have to offer you.");
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                }
            }
            else
            {
                //Invalid/Unsupported Query
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmAppointmentStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (stepContext.Result is FoundChoice vinChoice)
            {
                var vehicle = Services.CarStorage.ExecuteQuery(Services.CarStorage.CreateQuery<DB_Car>().Where(c => c.RowKey == vinChoice.Value) as TableQuery<DB_Car>).FirstOrDefault();

                if (vehicle != null)
                {
                    await stepContext.Context.SendActivityAsync($"I'm a {vehicle.Make} guy myself, good choice! I've marked down your interest for the VIN {vehicle.VIN}.");
                }

            }
            else
            {
                await stepContext.Context.SendActivityAsync("Thanks for filling that out!");
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

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
