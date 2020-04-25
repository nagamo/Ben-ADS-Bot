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

                NewUsedStep,
                ValidateNewUsedStep,

               //InterestStep,
                //ValidateInterestStep,

                VehicleTypeStep,
                ValidateTypeStep,

                VehicleMakeStep,
                ValidateVehicleMakeStep,

                VehicleModelStep,
                ValidateVehicleModelStep,

                //NeedFinancingStep,
                //ExecuteFinancingStep,

                VehicleColorStep,
                ValidateVehicleColorStep,
                //ExecuteTradingInStep,

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
                userData.VehicleProfile = new VehicleProfileDetails();
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }





        private async Task<DialogTurnResult> GoalsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Goals)) return await stepContext.NextAsync(cancellationToken: cancellationToken);


            //var goalOptions = Utilities.CreateOptions(new string[] { "Buy", "Lease", "Not Sure" }, "What are you looking to do?");
            //return await stepContext.PromptAsync(nameof(ChoicePrompt), goalOptions, cancellationToken);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("So, before we dive into the details, " + userData.Name + ", can you give me some sense of your goals? " +
                                             "For instance, are you seriously looking to buy / lease? Maybe just browsing? Come on, spill the beans!")
            });
        }

        private async Task<DialogTurnResult> ValidateGoalsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                //userData.VehicleProfile.Goals = Utilities.ReadChoiceWithManual(stepContext);
                userData.VehicleProfile.Goals = (string)stepContext.Result;
            return await stepContext.NextAsync();
        }



        /*private async Task<DialogTurnResult> InterestStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
        }*/



        private async Task<DialogTurnResult> VehicleTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Type)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var typeOptions = Utilities.CreateOptions(new string[] { "SUV", "Sedan", "Pickup", "EV", "Other" }, "Thanks! So, let's get started, shall we? " +
                                                      "First of all, What kind of vehicle are you primarily interested in?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), typeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Type = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }


        private async Task<DialogTurnResult> NewUsedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.NewUsed)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var newusedOptions = Utilities.CreateOptions(new string[] { "Yes!", "Nah", "Not sure" },
                                                        "OK, cool! Are you after that brand-new smell?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), newusedOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateNewUsedStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.NewUsed = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }



        private async Task<DialogTurnResult> VehicleMakeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Make)) return await stepContext.NextAsync(cancellationToken: cancellationToken);



            var makeOptions = Utilities.CreateOptions(new string[] { "Chevrolet", "Toyota", "Honda", "GMC", "Dodge", "Ford" }, 
                                                       "What manufacturer really floats your boat?");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), makeOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateVehicleMakeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                userData.VehicleProfile.Make = Utilities.ReadChoiceWithManual(stepContext);

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> VehicleModelStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Model)) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            //var modelOptions = Utilities.CreateOptions(new string[] { "< $1000", "$1000-5000", "$5000+" }, "Roughly what is your budget?");

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("OK, sounds good to me! If you've got a particular model in mind, go ahead and let me know. ")
            });

            //return await stepContext.PromptAsync(nameof(ChoicePrompt), brandOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ValidateVehicleModelStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
                //userData.VehicleProfile.Goals = Utilities.ReadChoiceWithManual(stepContext);
                userData.VehicleProfile.Model = (string)stepContext.Result;
            return await stepContext.NextAsync();
        }


        private async Task<DialogTurnResult> VehicleColorStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);
            if (!string.IsNullOrEmpty(userData.VehicleProfile.Color)) return await stepContext.NextAsync(cancellationToken: cancellationToken);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Last Question! Got a color in mind?")
            });
        }

        private async Task<DialogTurnResult> ValidateVehicleColorStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);

            if (stepContext.Result != null)
                userData.VehicleProfile.Color = (string)stepContext.Result;
            return await stepContext.NextAsync();
        }

        /*private async Task<DialogTurnResult> ExecuteFinancingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
         */


        private async Task<DialogTurnResult> FinalizeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await _userProfileAccessor.GetAsync(stepContext.Context);

            var lines = new List<string>
            {
                $"Thanks {userData.Name}! Let's summarize:",
                $"Your basic goals: {userData.VehicleProfile.Goals}",
                $"Looking for a new vehicle: {userData.VehicleProfile.NewUsed}",
                $"Type of vehicle: {userData.VehicleProfile.Type}",
                $"And you're obviously a '{userData.VehicleProfile.Make}' person!",
                $"Interested in a:  {userData.VehicleProfile.Model}",
                $"Preferably in: {userData.VehicleProfile.Color}"
            };

            /*lines.Add($"Financing?: {userData.VehicleProfile.NeedFinancing}");
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
            }*/

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Join(Environment.NewLine, lines)), cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
