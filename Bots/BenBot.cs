using ADS.Bot.V1.Dialogs;
using ADS.Bot.V1.Models;
using ADS.Bot.V1.Services;
using ADS.Bot1;
using ADS.Bot1.Dialogs;
using ADS_Sync;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Bots
{
    public class BenBot : ActivityHandler
    {
        private const string WelcomeSimple = "Hey there! I'm Chad. Welcome!";
        private const string WelcomeMeeting = "Hey there {0}! My name is, Chad, It's nice to meet you. Let's get started!";
        private const string WelcomePersonal = "Hey {0}! It's me, Chad. Let's get started!";
        private const string WelcomeReturn = "Welcome back {0}! Wasn't sure when we would talk again. What can I help you with today?";

        private DialogManager DialogManager;

        public ADSBotServices Services { get; }
        public UserState User { get; }
        public CRMCommitService CRMCommit { get; }



        public BenBot(ADSBotServices services, ActiveLeadDialog dialog, UserState user, CRMCommitService crmCommit)
        {
            Services = services;
            User = user;
            CRMCommit = crmCommit;
            DialogManager = new DialogManager(dialog)
            {
                UserState = user
            };
        }

        //Handle first-time user state, before the update is delegate to any other methods.
        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);
            if (userProfile.Details == null)
            {
                if (!string.IsNullOrWhiteSpace(turnContext.Activity.From.Name))// && turnContext.Activity.From.Name != "User")
                {
                    userProfile.Details = new Models.BasicDetails()
                    {
                        Name = turnContext.Activity.From.Name,
                        UniqueID = turnContext.Activity.From.Id,
                        New = true
                    };
                }

                var pageID = turnContext.Activity.ChannelId;
                var dealerData = Services.DataService.GetDealerByFacebookPageID(pageID);

                if (dealerData != null)
                {
                    userProfile.Details.DealerID = dealerData.RowKey;
                }

                if (!string.IsNullOrEmpty(userProfile.Details.DealerID))
                {
                    await Services.DealerConfig.RefreshDealerAsync(userProfile.Details.DealerID);
                }
                else if (Services.Configuration.GetValue<string>("bb:test_dealer") != null)
                {
                    //If we don't have a user-assigned dealer ID and we have a test one in config file, use that
                    userProfile.Details.DealerID = Services.Configuration.GetValue<string>("bb:test_dealer");
                }
            }
            CRMCommit.UpdateUserResponseTimeout(OnUserConversationExpired, userProfile, turnContext, cancellationToken);

            await base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        // This is called when a user is added to the conversation. This occurs BEFORE they've typed something in, so it's a good way to
        //initiate the conversation from the bot's perspective
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    //Print a personalized hello when we have their information already
                    //And even more "friendly" when we have already converted them as a lead before
                    if(userProfile.ADS_CRM_ID.HasValue)
                    {
                        await turnContext.SendActivityAsync(string.Format(WelcomeReturn, userProfile.FirstName), cancellationToken: cancellationToken);
                    }
                    else if(userProfile.Details.New)
                    {
                        await turnContext.SendActivityAsync(string.Format(WelcomeMeeting, userProfile.FirstName), cancellationToken: cancellationToken);
                    }
                    else if(userProfile.FirstName != null)
                    {
                        await turnContext.SendActivityAsync(string.Format(WelcomePersonal, userProfile.FirstName), cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(WelcomeSimple, cancellationToken: cancellationToken);
                    }
                    await DialogManager.OnTurnAsync(turnContext, cancellationToken);
                }
            }

            if(bool.TryParse(Services.Configuration["ads:debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }

            await Services.UserProfileAccessor.SetAsync(turnContext, userProfile, cancellationToken);
        }

        //Just for debug right now
        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            if (bool.TryParse(Services.Configuration["ads:debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }
        }

        //Happens after user hasn't responded in a duration configured by the dealer
        protected async Task OnUserConversationExpired(ITurnContext turnContext)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext);

            Services.CRM.WriteCRMDetails(CRMStage.Fnalize, userProfile);
        }

        //Handles incomming messages from users.
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (bool.TryParse(Services.Configuration["ads:debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }

            //Get the latest version
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);



            if (CheckForFacebookMarketplace(turnContext, userProfile))
            {
                //Function modifies incomming message, which should go to LUIS
                //VIN is also stored which is used later.

                if (turnContext.Activity.Text.Contains(Constants.FB_STILL_AVAILABLE))
                {
                    DB_Car matchingVehicle = null;

                    try
                    {
                        matchingVehicle = Services.DataService.GetCar(userProfile.SimpleInventory.VIN);
                    }
                    catch { }

                    if(matchingVehicle!= null)
                    {
                        userProfile.SimpleInventory.Make = matchingVehicle.Make;
                        userProfile.SimpleInventory.Model = matchingVehicle.Model;
                        userProfile.SimpleInventory.Year = matchingVehicle.Year;
                        userProfile.SimpleInventory.Used = matchingVehicle.Used;

                        await turnContext.SendActivityAsync("Good news! Looks like that vehicle is still in stock.");
                    }
                    else
                    {
                        await turnContext.SendActivityAsync("I don't see that vehicle in my inventory right now, but one of salesmen would be happy to set the record straight.");
                    }
                }

                //Set it null so LUIS doesn' interpret the text.
                turnContext.Activity.Text = null;
            }

            //Let the manager handle passing our message to the one-and-only dialog
            var dialogResult = await DialogManager.OnTurnAsync(turnContext, cancellationToken);
            await Services.SaveUserProfileAsync(userProfile, turnContext, cancellationToken);

            //This doesn't really happen, plus we have timeouts.
            switch (dialogResult.TurnResult.Status)
            {
                case DialogTurnStatus.Complete:
                    //End of conversation here....
                    if(dialogResult.TurnResult.Result != null)
                    {

                    }
                    break;
            }
        }

        //Handle parsing of facebook messages
        protected bool CheckForFacebookMarketplace(ITurnContext<IMessageActivity> turnContext, UserProfile userProfile)
        {
            var messageText = turnContext.Activity.Text;

            /* Example:
             * 
                https://www.facebook.com/marketplace/item/2904343352947650/?ref=messaging_thread&link_ref=BuyerBridge

                VIN: WDYPF4CC2B5509284

                Is this still available?
             * 
             */
            var matchRegex = Services.Configuration["ads:fb_message_regex"];
            if (string.IsNullOrEmpty(matchRegex)) return false;

            var matchTest = Regex.Match(messageText, matchRegex);

            if (matchTest.Success)
            {
                var fbURL = matchTest.Groups[1].Value;
                var vin = matchTest.Groups[4].Value;
                var query = matchTest.Groups[5].Value;

                userProfile.SimpleInventory = new SimpleInventoryDetails()
                {
                    VIN = vin
                };

                //Overwrite the activity text, so it's only the last line(s)
                turnContext.Activity.Text = query;

                return true;
            }

            return false;
        }
    }
}
