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

        const string CurrentFieldName = "CurrentField";


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

                RemainingGoalsStep,//Repeatable...
                ValidateRemainingGoalsStep, //Continue on or repeat

                AdditionalOptions, //If repeating, show options
                ValidateAdditonalOptions, //Tell how many results and go back to remaining goals to cycle

                ShowInventoryStep, //When user requests listing, show them cars. This needs to cycle too...(pages/reset filters/etc.)

                ConfirmAppointmentStep,
                FinalizeStep
            };

            //Examples...
            //New -> Price -> 10k -> Inventory
            //New -> Price -> 10k -> Make -> Honda
            //New -> Make -> Honda -> Model -> Civic -> Inventory


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

            if (!string.IsNullOrEmpty(UserData.Inventory.Make))
            {
                carQuery = carQuery.Where(c => c.Make == UserData.Inventory.Make);
            }
            if (!string.IsNullOrEmpty(UserData.Inventory.Model))
            {
                carQuery = carQuery.Where(c => c.Model == UserData.Inventory.Model);
            }
            if (!string.IsNullOrEmpty(UserData.Inventory.Color))
            {
                carQuery = carQuery.Where(c => c.Color == UserData.Inventory.Color);
            }
            if (UserData.Inventory.MinPrice.HasValue && UserData.Inventory.MaxPrice.HasValue)
            {
                carQuery = carQuery.Where(c => c.Price >= UserData.Inventory.MinPrice.Value && c.Price <= UserData.Inventory.MaxPrice.Value);
            }
            else if (UserData.Inventory.MinPrice.HasValue)
            {
                carQuery = carQuery.Where(c => c.Price >= UserData.Inventory.MinPrice.Value);
            }
            else if (UserData.Inventory.MaxPrice.HasValue)
            {
                carQuery = carQuery.Where(c => c.Price <= UserData.Inventory.MaxPrice.Value);
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

            var newUsedCounts = BuyerBridgeService.ListAvailableNewUsed(Services, null);

            if(newUsedCounts.Count() == 0)
            {
                //Indicates no Used *and* New cars, so skip this
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }
            else
            {
                var newUsedOptions = Utilities.GroupedOptions(newUsedCounts, "Are you shopping new or used?");
                return await stepContext.PromptAsync(nameof(ChoicePrompt), newUsedOptions, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> NewUsedConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (stepContext.Result is FoundChoice newUsedChoice)
            {
                userData.Inventory.NewUsed = Utilities.CleanGroupedOption(newUsedChoice.Value);

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

            var concernOptions = Utilities.CreateOptions(userData.Inventory.MissingFields().Append("Nothing Specific"), "What is your primary concern regarding a vehicle puchase?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), concernOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidatePrimaryConcernStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result is FoundChoice choice)
            {
                userData.Inventory.PrimaryConcern = Utilities.ReadChoiceWithManual(stepContext);
                stepContext.Values[CurrentFieldName] = choice.Value;
            }

            return await stepContext.NextAsync();
        }










        private async Task<DialogTurnResult> RemainingGoalsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            //If we have a field from primary concern, skip asking this time arund.
            if (stepContext.Values.ContainsKey(CurrentFieldName))
                return await stepContext.NextAsync(cancellationToken: cancellationToken);

            var missingFields = userData.Inventory.MissingFields();


            //If we have no more remaining fields to fill skip the prompt
            //Also skip if user ever narrows down to a single entry, automatically advance them
            if (missingFields.Count == 0 || stepContext.Context.Activity.Text.EndsWith(" (1)"))
            {
                stepContext.Values[CurrentFieldName] = "Show Cars";
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            var remainingOptions = Utilities.CreateOptions(missingFields.Prepend("Show Cars"), "Do you want to see those cars right now, or would you like to add another filter?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), remainingOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateRemainingGoalsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            if (stepContext.Result is FoundChoice choice)
                stepContext.Values[CurrentFieldName] = choice.Value;

            return await stepContext.NextAsync();
        }


        private async Task<DialogTurnResult> AdditionalOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if (!stepContext.Values.ContainsKey(CurrentFieldName))
                return await stepContext.NextAsync(cancellationToken: cancellationToken);

            PromptOptions goalOptions = null;

            switch (stepContext.Values[CurrentFieldName])
            {
                case "Max Price":
                    goalOptions = Utilities.GroupedOptions(BuyerBridgeService.ListAvailablePriceMaxes(Services, BuildQuery(userData)),
                        "What is your upper limit on a car?");
                    break;
                case "Price Range":
                    goalOptions = Utilities.GroupedOptions(BuyerBridgeService.ListAvailablePriceRanges(Services, BuildQuery(userData)),
                        "What price range are you looking at?");
                    break;
                case nameof(VehicleInventoryDetails.Make):
                    goalOptions = Utilities.GroupedOptions(BuyerBridgeService.ListAvailableMakes(Services, BuildQuery(userData)),
                        "What make are you in store for?");
                    break;
                case nameof(VehicleInventoryDetails.Model):
                    goalOptions = Utilities.GroupedOptions(BuyerBridgeService.ListAvailableModels(Services, BuildQuery(userData)),
                        $"What kind of {userData.Inventory.Make} are you looking for?");
                    break;
                case nameof(VehicleInventoryDetails.Color):
                    goalOptions = Utilities.GroupedOptions(BuyerBridgeService.ListAvailableColors(Services, BuildQuery(userData)),
                        "What is your color preference?");
                    break;
                case "Nothing Specific":
                    goalOptions = Utilities.CreateOptions(new string[] { "Yes!", "Not Exactly..", "Just looking" }, "Do you know what kind of vehicle you want?");
                    break;
                case "Show Cars":
                    //Bit of a hack here to jump forward in the waterfall to the display section.
                    //Ideally the parameter setting is its own dialog, along with inventory display, for cleaner looping
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] + 1;
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                default:
                    //?
                    break;
            }

            if (goalOptions != null)
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt), goalOptions, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }
        }
        private async Task<DialogTurnResult> ValidateAdditonalOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);

            if(stepContext.Result is FoundChoice selectedOption)
            {
                //Handle the results coming back, this should really be regex or something more reliable...
                var cleanAnswer = Utilities.CleanGroupedOption(selectedOption.Value);

                switch (stepContext.Values[CurrentFieldName])
                {
                    case nameof(VehicleInventoryDetails.Make):
                        userData.Inventory.Make = cleanAnswer;
                        break;
                    case nameof(VehicleInventoryDetails.Model):
                        userData.Inventory.Model = cleanAnswer;
                        break;
                    case nameof(VehicleInventoryDetails.Color):
                        userData.Inventory.Color = cleanAnswer;
                        break;
                    //Price is special case.
                    case "Max Price":
                    case "Price Range":
                        userData.Inventory.ParsePrices(cleanAnswer);
                        break;
                }
            }

            //If we've made it to this point, the user has supplied a parameter (repeat or not)
            //This means we need to jump back and prompt them if they'd like to supply an additional parameter
            //Also need to delete the key in the state data so we ask again
            stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 4;
            stepContext.Values.Remove(CurrentFieldName);
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }







        private async Task<DialogTurnResult> ShowInventoryStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);


            var carQuery = BuildQuery(userData);

            if (carQuery != null)
            {
                var results = Services.CarStorage.ExecuteQuery<DB_Car>(carQuery).ToList();
                var resultsCount = results.Count();

                if (resultsCount > 0)
                {
                    var trimmedResults = results.Take(15);

                    if(trimmedResults.Count() != resultsCount)
                    {
                        await stepContext.Context.SendActivityAsync($"Here are the top {trimmedResults.Count()} cars I was able to find for you.");
                    }
                    else
                    {
                        if(resultsCount == 1)
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

                        if(car_result.Price > 0)
                        {
                            card.Text = $"{car_result.Price:C2}";
                        }

                        if (car_result.Used)
                        {
                            card.Subtitle = $"Used: {car_result.Mileage:N0} Miles. {car_result.Engine}, {car_result.Doors} Door";
                        }
                        else
                        {
                            card.Subtitle = $"New. {car_result.Engine}, {car_result.Doors} Door";
                        }

                        return card.ToAttachment();
                    }).ToList();

                    var CARouselActivity = Utilities.CreateCarousel(carAttachments);

                    //Supply the options here so the prompt code can line up out postback values to our list of VINs
                    var carOptions = Utilities.CreateOptions(trimmedResults.Select(c => c.VIN), CARouselActivity as Activity, Style: ListStyle.None);
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
