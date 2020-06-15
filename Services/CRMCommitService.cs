using ADS.Bot.V1.Models;
using ADS.Bot1;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class CRMCommitService : IHostedService
    {
        Dictionary<string, UserMessageState> UserTimeouts = new Dictionary<string, UserMessageState>();

        public CRMCommitService(ADSBotServices botServices)
        {
            BotServices = botServices;
        }

        public ADSBotServices BotServices { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void UpdateUserResponseTimeout(Func<ITurnContext, Task> timeoutAction, UserProfile userProfile, ITurnContext context
            , CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userProfile?.Details?.UniqueID))
            {
                return;
            }

            if (UserTimeouts.ContainsKey(userProfile.Details.UniqueID))
            {
                var existingTimer = UserTimeouts[userProfile.Details.UniqueID];

                existingTimer.ResponseCancel.Cancel();
                try
                {
                    existingTimer.ResponseTimeout.Wait();
                }
                catch(Exception ex)
                    when (((ex as AggregateException)?.InnerException ?? ex) is TaskCanceledException)
                {
                    //Ignore cancel exceptions.
                }
                

                UserTimeouts.Remove(userProfile.Details.UniqueID);
            }

            var newCancel = new CancellationTokenSource();

            var timeout = TimeSpan.FromSeconds(BotServices.DealerConfig.Get<int>(userProfile, "conversation_timeout", 10));

            UserTimeouts[userProfile.Details.UniqueID] = new UserMessageState()
            {
                ResponseCancel = newCancel,
                ResponseTimeout = Utilities.DelayTask(() =>
                {
                    timeoutAction(context).Wait();
                }, timeout, newCancel.Token)
            };
        }

        internal class UserMessageState
        {
            internal Task ResponseTimeout { get; set; }
            internal CancellationTokenSource ResponseCancel { get; set; }
        }
    }
}
