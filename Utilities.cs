using AdaptiveCards;
using ADS.Bot.V1.Cards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1
{
    public class Utilities
    {

        public static PromptOptions CreateOptions(string[] Options, string PromptText, string RetryText = null)
        {
            return CreateOptions(Options, MessageFactory.Text(PromptText), MessageFactory.Text(RetryText));
        }

        public static PromptOptions CreateOptions(string[] Options, Activity Prompt, Activity Retry = null)
        {
            var promptOpts = new PromptOptions();

            promptOpts.Choices = Options.Select(opt =>
            {
                return new Microsoft.Bot.Builder.Dialogs.Choices.Choice(opt);
            }).ToList();

            promptOpts.Prompt = Prompt;
            promptOpts.RetryPrompt = Retry;

            return promptOpts;
        }

        public static string ReadChoiceWithManual(WaterfallStepContext stepContext)
        {
            if(stepContext.Result is FoundChoice choice)
            {
                if(choice.Value != stepContext.Context.Activity.Text)
                {
                    return stepContext.Context.Activity.Text;
                }
                else { return choice.Value; }
            }
            else
            {
                return stepContext.Result?.ToString();
            }
        }

        public static Attachment CreateAttachment(AdaptiveCard Card)
        {
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Card))
            };
        }

        public static CodeAction CardFactoryAction<TModel>(ICardFactory<TModel> Factory)
        {
            return new CodeAction(async (context, obj) =>
            {
                var initData = Factory.Populate(context.Context);

                var message = Activity.CreateMessageActivity();

                message.Attachments = new List<Attachment>()
                {
                    CreateAttachment(Factory.CreateCard(initData, context.Context))
                };
                await context.Context.SendActivityAsync(message);

                return new DialogTurnResult(DialogTurnStatus.Complete, null);
            });
        }

        public static bool AttemptParseCardResult<T>(ITurnContext context, out T result)
        {
            result = default;
            var data = context.Activity.ChannelData as JObject;
            if (Convert.ToBoolean(data.Value<string>("postBack")))
            {
                result = (context.Activity.Value as JObject).ToObject<T>();
                return result != null;
            }
            else
            {
                return false;
            }
        }
    }
}
