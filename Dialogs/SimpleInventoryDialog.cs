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
    public class SimpleInventoryDialog : ComponentDialog
    {

        const string InventoryChoice = "INVENTORY";

        public ADSBotServices Services { get; }
        public DataService DataService { get; }

        public SimpleInventoryDialog(ADSBotServices services, DataService dataService)
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

                MakeStep,
                ValidateMakeStep,

                ModelStep,
                ValidateModelStep,

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
            DataService = dataService;
        }

        private TableQuery<DB_Car> BuildQuery(UserProfile UserData)
        {
            if (UserData.SimpleInventory.PrimaryConcern == "Nothing Specific")
                return null;

            IQueryable<DB_Car> carQuery = DataService.CreateCarQuery();

            if (!string.IsNullOrEmpty(UserData.SimpleInventory.ConcernGoal))
            {
                switch (UserData.SimpleInventory.PrimaryConcern)
                {
                    case "By Price":

                        if (UserData.SimpleInventory.ConcernGoal == "Any Price")
                        {
                            //TODO: Something
                        }
                        else
                        {
                            var priceRange = Utilities.ParsePrices(UserData.SimpleInventory.ConcernGoal);


                            if (priceRange.MinPrice.HasValue && priceRange.MaxPrice.HasValue)
                            {
                                carQuery = carQuery.Where(c => c.Price >= priceRange.MinPrice.Value && c.Price <= priceRange.MaxPrice.Value);
                            }
                            else if (priceRange.MinPrice.HasValue)
                            {
                                carQuery = carQuery.Where(c => c.Price >= priceRange.MinPrice.Value);
                            }
                            else if (priceRange.MaxPrice.HasValue)
                            {
                                carQuery = carQuery.Where(c => c.Price <= priceRange.MaxPrice.Value);
                            }
                            else
                            {
                                carQuery = null;
                            }
                        }
                        break;
                    case "By Payment":
                        if(UserData.SimpleInventory.ConcernGoal == "Any Payment")
                        {
                            //TODO: Something
                        }
                        else
                        {
                            var payment = Utilities.ParsePrices(UserData.SimpleInventory.ConcernGoal);
                            carQuery = carQuery.Where(c => c.Price <= Utilities.CalculatePayment(payment.MaxPrice.Value));
                        }
                        break;
                    case "By Vehicle Type":
                        carQuery = carQuery.Where(c => c.Body == UserData.SimpleInventory.ConcernGoal);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(UserData.SimpleInventory.Make))
            {
                carQuery = carQuery.Where(c => c.Make == UserData.SimpleInventory.Make);
            }
            if (!string.IsNullOrEmpty(UserData.SimpleInventory.Model))
            {
                carQuery = carQuery.Where(c => c.Model == UserData.SimpleInventory.Model);
            }

            return carQuery as TableQuery<DB_Car>;
        }


        private async Task<DialogTurnResult> PreInitializeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (userData?.SimpleInventory != null)
            {
                if (userData.SimpleInventory.IsCompleted)
                {
                    var resetOptions = Utilities.CreateOptions(new string[] { "Reset", "Use Previous" },
                        "Look's like I've already got inventory details for you.\r\nWould you to fill those details out again?",
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
                userData.SimpleInventory = new SimpleInventoryDetails();

                //Select as little data as possible
                var foundCars = DataService.CountCars(BuildQuery(userData));
                await stepContext.Context.SendActivityAsync($"I'm glad you asked about my inventory. I just so happen to have {foundCars:n0} cars available!");

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
                        userData.SimpleInventory = new SimpleInventoryDetails();
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
            if (userData.SimpleInventory.SkipPrimaryConcern) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            var concernOptions = Utilities.CreateOptions(new string[] { "By Price", "By Payment", "By Vehicle Type" }, "How would you like to shop?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), concernOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidatePrimaryConcernStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result != null)
                userData.SimpleInventory.PrimaryConcern = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> ConcernGoalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.SimpleInventory.SkipConcernGoal) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            PromptOptions goalOptions = null;

            var currentQuery = BuildQuery(userData);
            var dealerShowCount = await Services.DealerConfig.GetAsync<bool>(userData, "show_count", true);

            switch (userData.SimpleInventory.PrimaryConcern)
            {
                case "By Price":
                    goalOptions = Utilities.GroupedOptions(DataService.ListAvailablePriceRanges(currentQuery),
                        "Sure thing! Name your price range", ExtraAppend: "Any Price", ShowCount: dealerShowCount);
                    break;
                case "By Payment":
                    goalOptions = Utilities.GroupedOptions(DataService.ListAvailablePayments(currentQuery),
                        "Sure thing! Name your payment", ExtraAppend: "Any Payment", ShowCount: dealerShowCount);
                    break;
                case "By Vehicle Type":
                    goalOptions = Utilities.GroupedOptions(DataService.ListAvailableBodyTypes(currentQuery),
                        "Great! What vehicle type are you interested in?", ShowCount: dealerShowCount);
                    break;
            }

            if (goalOptions != null)
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt), goalOptions, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions() { Prompt = MessageFactory.Text("How can we help with that concern?") }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ValidateConcernStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result is FoundChoice concernChoice)
            {
                userData.SimpleInventory.ConcernGoal = Utilities.CleanGroupedOption(concernChoice.Value);
            }

            return await stepContext.NextAsync();
        }






        private async Task<DialogTurnResult> MakeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.SimpleInventory.SkipMake) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            var dealerShowCount = await Services.DealerConfig.GetAsync<bool>(userData, "show_count", true);

            var concernOptions = Utilities.GroupedOptions(DataService.ListAvailableMakes(BuildQuery(userData)), 
                "Here’s what we have",
                ExtraAppend: Constants.MSG_DONT_SEE_IT, ShowCount: dealerShowCount);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), concernOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateMakeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result is FoundChoice makeChoice)
            {
                if (makeChoice.Value == Constants.MSG_DONT_SEE_IT)
                {
                    //Something...
                }
                else
                {
                    userData.SimpleInventory.Make = Utilities.CleanGroupedOption(makeChoice.Value);
                }
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }
        private async Task<DialogTurnResult> ModelStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (userData.SimpleInventory.SkipModel) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            var dealerShowCount = await Services.DealerConfig.GetAsync<bool>(userData, "show_count", true);

            var concernOptions = Utilities.GroupedOptions(DataService.ListAvailableModels(BuildQuery(userData)),
                "Here’s what we have",
                ExtraAppend: Constants.MSG_DONT_SEE_IT, ShowCount: dealerShowCount);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), concernOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateModelStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result is FoundChoice modelChoice)
            {
                if (modelChoice.Value == Constants.MSG_DONT_SEE_IT)
                {
                    //Something...
                }
                else
                {
                    userData.SimpleInventory.Model = Utilities.CleanGroupedOption(modelChoice.Value);
                }
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }







        private async Task<DialogTurnResult> ShowInventoryStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);


            var carQuery = BuildQuery(userData);

            if (carQuery != null)
            {
                var results = DataService.GetCars(carQuery);
                var resultsCount = results.Count();

                if (resultsCount >= 100)
                {
                    //100+
                    await stepContext.Context.SendActivityAsync($"Great news {userData.FirstName}! I've actually got {resultsCount:n0} cars that match that {userData.SimpleInventory.PrimaryConcern.ToLower()}!");
                }
                else if (resultsCount >= 10)
                {
                    //10-100
                    await stepContext.Context.SendActivityAsync($"I was able to find {resultsCount:n0} cars for that {userData.SimpleInventory.PrimaryConcern.ToLower()} {userData.FirstName}.");
                }
                else if (resultsCount >= 1)
                {
                    //1-10
                    await stepContext.Context.SendActivityAsync($"I found {resultsCount:n0} cars that match that {userData.SimpleInventory.PrimaryConcern.ToLower()}\r\nI know its not a lot, but we've got plenty of other vehicles available!");
                }

                if (resultsCount > 0)
                {
                    var trimmedResults = results.Take(25);

                    if (trimmedResults.Count() != resultsCount)
                    {
                        await stepContext.Context.SendActivityAsync($"Here are the top {trimmedResults.Count()} cars I was able to find for you.");
                    }
                    else
                    {
                        if (resultsCount == 1)
                        {
                            await stepContext.Context.SendActivityAsync($"This is the only car I've got matching all those options!");
                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync($"Take a look at these {resultsCount} cars and see what you think.");
                        }
                    }

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
                                    Type = ActionTypes.PostBack,
                                    Title = "This is it!",
                                    Text = $"I like #{car_result.VIN()}",
                                    Value = car_result.VIN()
                                }
                            }
                        };

                        //Don't always have an image.
                        if (!string.IsNullOrEmpty(car_result.URL))
                        {
                            card.Buttons.Add(
                                new CardAction()
                                {
                                    Type = ActionTypes.OpenUrl,
                                    Title = "Full details",
                                    Value = car_result.URL
                                });
                        }

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

                    //Append a final card just in case
                    carAttachments = carAttachments.Append(new HeroCard()
                    {
                        Title = "Not here.",
                        Text = "If you can't quite find what you are looking for here, one of our salesman would love to get in touch to follow up.",
                        Buttons = new List<CardAction>()
                        {
                            new CardAction(){
                                Type = ActionTypes.PostBack,
                                Title = Constants.MSG_DONT_SEE_IT,
                                Value = Constants.MSG_DONT_SEE_IT
                            }
                        }
                    }.ToAttachment());

                    var CARouselActivity = Utilities.CreateCarousel(carAttachments);

                    var availableOptions = trimmedResults.Select(c => c.VIN()).Append(Constants.MSG_DONT_SEE_IT);
                    var carOptions = Utilities.CreateOptions(availableOptions, CARouselActivity as Activity, Style: ListStyle.None);
                    return await stepContext.PromptAsync(InventoryChoice, carOptions, cancellationToken: cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync($"I'm sorry, I dont actually seem to have any cars that match that {userData.SimpleInventory.PrimaryConcern.ToLower()}.\r\nWe'd still love to get in touch to explore what vehicles we have to offer you.");
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
                if(vinChoice.Value == Constants.MSG_DONT_SEE_IT)
                {
                    await stepContext.Context.SendActivityAsync($"No worries! One of our salesmen would be happy to get in touch with you to help narrow down on a vehicle.");
                }
                else
                {
                    userData.SimpleInventory.VIN = vinChoice.Value;

                    var vehicle = DataService.GetCar(vinChoice.Value);
                    if (vehicle != null)
                    {
                        await stepContext.Context.SendActivityAsync($"I'm a {vehicle.Make} guy myself, good choice! I've marked down your interest for the VIN {vehicle.VIN()}.");
                    }
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Thanks for filling that out!");
            }


            if (Services.CRM.IsActive)
            {
                var appointmentOptions = Utilities.CreateOptions(new string[] { "Yes!", "No" }, "Would you like a salesman to contact you?");
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
                if (stepContext.Result is FoundChoice appointmentChoice)
                {
                    userData.Details.RequestContact = (appointmentChoice.Value == "Yes!");
                }

                Services.CRM.WriteCRMDetails(CRMStage.SimpleInventoryCompleted, userData);

                if (userData.Details.RequestContact)
                {
                    await stepContext.Context.SendActivityAsync("Thanks! Someone will be in touch with you shortly.");
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("Thanks for filling that out, I'll remember your details in case you want to come back and make an appointment later.");
                }
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
