// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ADS.Bot1
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, IStorage storage, UserState userState, ConversationState conversationState = null)
            : base(configuration, logger)
        {
            this.UseStorage(storage);
            this.UseState(userState, conversationState);
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                // Send a message to the user
                await turnContext.SendActivityAsync("Sorry! An error has occurred.");

                if (conversationState != null)
                {
                    try
                    {
                        // Delete the conversationState for the current conversation to prevent the
                        // bot from getting stuck in a error-loop caused by being in a bad state.
                        // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                        await conversationState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Exception caught on attempting to Delete ConversationState : {e.Message}");
                    }
                }

                var debug_messages = bool.TryParse(configuration["ads:debug_messages"], out var msg_debug) && msg_debug;
                var debug_errors = bool.TryParse(configuration["ads:debug_errors"], out var err_debug) && err_debug;

                if (debug_messages || debug_errors)
                {
                    //if (turnContext.Activity.ChannelId == "emulator")
                    if(exception is Microsoft.Bot.Schema.ErrorResponseException erex)
                    {
                        await turnContext.SendActivityAsync($"Error Reponse\r\n{erex.Request.Content}\r\n{erex.Message}\r\n{JsonConvert.SerializeObject(erex.Body)}\r\n{JsonConvert.SerializeObject(erex.Data)}");
                    }
                    else
                    {
                        await turnContext.SendActivityAsync($"{exception.GetType().FullName}\r\n{exception.Message}\r\n{exception.StackTrace}");
                    }
                }

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");
            };
        }
    }
}
