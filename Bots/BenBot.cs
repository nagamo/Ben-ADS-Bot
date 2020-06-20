﻿using ADS.Bot.V1.Dialogs;
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
using Newtonsoft.Json.Linq;
using RestSharp;
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
        private const string WelcomeSimple = "Hey there!";
        private const string WelcomeMeeting = "Hey there {0}!, It's nice to meet you.";
        private const string WelcomePersonal = "Hey {0}! Let's get started!";
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

        private async Task HandleUserData(ChannelAccount user, ITurnContext turnContext, bool forceSend, CancellationToken cancellationToken = default)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);
            if (userProfile.Details == null)
            {
                if (!string.IsNullOrWhiteSpace(user.Name))// && turnContext.Activity.From.Name != "User")
                {
                    userProfile.Details = new Models.BasicDetails()
                    {
                        Name = user.Name,
                        UniqueID = user.Id,
                        New = true
                    };
                }
                else
                {
                    //Facebook apparently doesn't send the user names
                    //Except maybe the developers? It works as expected for me
                    //Handled a little bit lower in this function
                    userProfile.Details = new Models.BasicDetails()
                    {
                        Name = "Chat User",
                        UniqueID = user.Id,
                        New = true
                    };
                }

            }

            //Handle user coming from facebook page, set their DealerID
            if (string.IsNullOrEmpty(userProfile.Details.DealerID))
            {
                var pageID = turnContext.Activity.Recipient.Id;
                var dealerData = Services.DataService.GetDealerByFacebookPageID(pageID);
                
                if (dealerData != null)
                {
                    userProfile.Details.DealerID = dealerData.RowKey;
                    Services.AI_Event("Dealer", userProfile, dealerData.Name);
                }
                else
                {
                    //Doesn't actually throw, just used to be error-level
                    Services.AI_Exception(new Exception($"Unable to find dealer for page ID {pageID}"), userProfile, pageID);
                }
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

            if (userProfile.Details.New)
            {
                //Here we handle the empty name from facebook, by querying the graph API and asking for full name
                //We can query additional fields here in the future.....
                if (turnContext.Activity.ChannelId == "facebook")
                {
                    try
                    {
                        var fbToken = Services.DealerConfig.Get<string>(userProfile, "fb_token");
                        if (fbToken != null)
                        {
                            var graphClient = new RestClient("https://graph.facebook.com/v7.0");
                            var profileQuery = new RestRequest(userProfile.Details.UniqueID, Method.GET, DataFormat.Json);
                            profileQuery.AddParameter("access_token", fbToken);
                            profileQuery.AddParameter("fields", "name");

                            var profileResult = await graphClient.ExecuteAsync(profileQuery, cancellationToken);
                            var profileObj = JObject.Parse(profileResult.Content);

                            var fullName = profileObj.Value<string>("name");
                            if (string.IsNullOrEmpty(fullName))
                            {
                                Services.AI_Event("FB_Lookup_Fail", userProfile, $"Got invalid/empty response from graph API: {profileResult.Content}");
                            }
                            else
                            {
                                Services.AI_Event("FB_Lookup", userProfile, $"Got user details: {profileResult.Content}");
                                userProfile.Details.Name = fullName;
                            }
                        }
                        else
                        {
                            Services.AI_Event("FB_Lookup_Fail", userProfile, $"No fb_token defined for dealer. Cannot query actual user name");
                        }
                    }
                    catch (Exception ex)
                    {
                        Services.AI_Exception(ex, userProfile, "Error while querying FB Graph API");
                    }
                }


                //Print a personalized hello when we have their information already
                if (userProfile.Details.Name == "Chat User")
                {
                    await turnContext.SendActivityAsync(WelcomeSimple, cancellationToken: cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync(string.Format(WelcomeMeeting, userProfile.FirstName), cancellationToken: cancellationToken);
                }

                Services.AI_Event("Chat_New_User", userProfile, $"Chatting via {turnContext.Activity.ChannelId}");
                userProfile.Details.New = false;
            }
            else if (forceSend)
            {
                if (userProfile.ADS_CRM_ID.HasValue)
                {
                    await turnContext.SendActivityAsync(string.Format(WelcomeReturn, userProfile.FirstName), cancellationToken: cancellationToken);
                }
                else
                {
                    if (userProfile.Details.Name == "Chat User")
                    {
                        await turnContext.SendActivityAsync(WelcomeSimple, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(string.Format(WelcomePersonal, userProfile.FirstName), cancellationToken: cancellationToken);
                    }
                }
            }


            await Services.SaveUserProfileAsync(userProfile, turnContext, cancellationToken);
            CRMCommit.UpdateUserResponseTimeout(OnUserConversationExpired, userProfile, turnContext, cancellationToken);
        }

        // This is called when a user is added to the conversation. This occurs BEFORE they've typed something in, so it's a good way to
        //initiate the conversation from the bot's perspective
        //NOTE: does not happen on facebook.
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await HandleUserData(member, turnContext, true, cancellationToken);



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

            Services.AI_Event("Chat_Timeout", userProfile);

            if (Services.CRM.IsActive)
            {
                Services.CRM.WriteCRMDetails(CRMStage.Fnalize, userProfile);
            }

            await Services.SaveUserProfileAsync(userProfile, turnContext);
        }

        //Handles incomming messages from users.
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (bool.TryParse(Services.Configuration["ads:debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }

            await HandleUserData(turnContext.Activity.From, turnContext, false, cancellationToken);

            //Get the latest version
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);

            if (CheckForFacebookMarketplace(turnContext, userProfile))
            {
                //Function modifies incomming message, which should go to LUIS
                //VIN is also stored which is used later.
                Services.AI_Event("FB_Marketplace_Source", userProfile, turnContext.Activity.Text);

                if (turnContext.Activity.Text.Contains(Constants.FB_STILL_AVAILABLE))
                {
                    DB_Car matchingVehicle = null;

                    try
                    {
                        matchingVehicle = Services.DataService.GetCar(userProfile.SimpleInventory.VIN);
                    }
                    catch (Exception ex)
                    {
                        Services.AI_Exception(ex, userProfile, "Error while checking vehicle availability");
                    }

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
            Services.AI_Event("Chat_Message", userProfile);

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
