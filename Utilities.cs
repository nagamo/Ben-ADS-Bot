using AdaptiveCards;
using ADS.Bot.V1.Cards;
using ADS.Bot.V1.Models;
using ADS.Bot1;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1
{
    public class Utilities
    {

        public static PromptOptions CreateOptions(IEnumerable<string> Options, string PromptText, string RetryText = null, ListStyle Style = ListStyle.SuggestedAction)
        {
            return CreateOptions(Options.Where(o => !string.IsNullOrEmpty(o)).ToArray(), MessageFactory.Text(PromptText), MessageFactory.Text(RetryText), Style);
        }

        public static PromptOptions CreateOptions(IEnumerable<string> Options, Activity Prompt, Activity Retry = null, ListStyle Style = ListStyle.SuggestedAction)
        {
            var promptOpts = new PromptOptions();

            promptOpts.Choices = Options.Select(opt =>
            {
                return new Choice(opt);
            }).ToList();

            promptOpts.Prompt = Prompt;
            promptOpts.RetryPrompt = Retry;
            promptOpts.Style = Style;

            return promptOpts;
        }

        public static PromptOptions GroupedOptions(IEnumerable<(string,int)> Groups, string PromptText, 
            string RetryText = null, string ExtraPrepend = null, string ExtraAppend = null, bool ShowCount = true)
        {
            var makeOptions = Groups.Select(mo => (ShowCount ? $"{mo.Item1} ({mo.Item2})" : $"{mo.Item1}")).Take(10);
            if (ExtraPrepend != null)
                makeOptions = makeOptions.Prepend(ExtraPrepend);
            if (ExtraAppend != null)
                makeOptions = makeOptions.Append(ExtraAppend);
            return CreateOptions(makeOptions, PromptText, RetryText);
        }

        internal static async Task<bool> AllChoiceValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            return true;
        }

        public static string CleanGroupedOption(string GroupText)
        {
            return GroupText.Split(" (", 2).First();
        }

        public static int CalculatePayment(int MonthlyPayment)
        {
            double Rate = 0.0467;   //APR
            double Term = 48;        //Months
            double TermRate = (1.0 - Math.Pow(1 + (Rate / 12), -Term));
            double FinalAmount = (TermRate * MonthlyPayment) / (Rate / 12);
            return (int)FinalAmount;
        }
        public static (int? MinPrice, int? MaxPrice) ParsePrices(string Input)
        {
            var enteredPrices = Regex.Matches(Input, @"([<]*\$?[<]*(\d+)[kK\+]*)");

            int? MaxPrice = null;
            int? MinPrice = null;

            if (enteredPrices.Count == 1)
            {
                var priceFilter = enteredPrices.First();
                var thousands = priceFilter.Value.Contains("k", StringComparison.OrdinalIgnoreCase);
                if (Input.Contains("<")) { MaxPrice = int.Parse(priceFilter.Groups[2].Value) * (thousands ? 1000 : 1); }
                else if (Input.Contains("+")) { MinPrice = int.Parse(priceFilter.Groups[2].Value) * (thousands ? 1000 : 1); }
                else { MaxPrice = int.Parse(priceFilter.Groups[2].Value) * (thousands ? 1000 : 1); }
            }
            else if (enteredPrices.Count >= 2)
            {
                //Bit ugly, but it works :)
                MinPrice = int.Parse(enteredPrices[0].Groups[2].Value) * (enteredPrices[0].Value.Contains("k", StringComparison.OrdinalIgnoreCase) ? 1000 : 1);
                MaxPrice = int.Parse(enteredPrices[1].Groups[2].Value) * (enteredPrices[1].Value.Contains("k", StringComparison.OrdinalIgnoreCase) ? 1000 : 1);
            }

            return (MinPrice, MaxPrice);
        }

        public static string ReadChoiceWithManual(WaterfallStepContext stepContext)
        {
            if (stepContext.Result is FoundChoice choice)
            {
                if (choice.Value != stepContext.Context.Activity.Text)
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

        public static List<Dialog> CardFactoryActions<TModel>(ICardFactory<TModel> Factory)
        {
            return new List<Dialog>()
            {
                CardFactoryAction<TModel>(Factory)
            };
        }
        public static CodeAction CardFactoryAction<TModel>(ICardFactory<TModel> Factory)
        {
            return new CodeAction(async (context, obj) =>
                {
                    var initData = await Factory.Populate(context.Context);

                    var message = Activity.CreateMessageActivity();

                    message.Attachments = new List<Attachment>()
                    {
                        CreateAttachment(Factory.CreateCard(initData, context.Context))
                    };
                    await context.Context.SendActivityAsync(message);

                    return new DialogTurnResult(DialogTurnStatus.Waiting, null);
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

        public static string CheckEvent(ITurnContext context)
        {
            return (context.TurnState["turn"] as JObject)?["dialogEvent"]?.Value<string>("name");
        }

        public static IMessageActivity CreateCarousel(IEnumerable<HeroCard> cards)
        {
            return CreateCarousel(cards.Select(c => c.ToAttachment()));
        }

        public static IMessageActivity CreateCarousel(IEnumerable<Attachment> attachments)
        {
            return MessageFactory.Carousel(attachments);
        }

        public static async Task<bool> ShouldSkipAsync(ADSBotServices Services, UserProfile Profile, string QuestionPath = "", bool AlreadyFilled = false)
        {
            var askQuestion = await Services.DealerConfig.GetAskQuestionAsync(Profile, QuestionPath);

            return !askQuestion || AlreadyFilled;
        }

        public static async Task<bool> ShouldSkipAsync(ADSBotServices Services, ITurnContext TurnContext, string QuestionPath = "", Func<UserProfile, bool> AlreadyFilled = null, CancellationToken cancellationToken = default)
        {
            var userData = await Services.GetUserProfileAsync(TurnContext, cancellationToken);
            return await ShouldSkipAsync(Services, userData, QuestionPath, AlreadyFilled?.Invoke(userData) ?? false);
        }

        public static bool ShouldSkip(ITurnContext TurnContext)
        {
            if (CheckEvent(TurnContext) == Constants.Event_Reject)
            {
                return true;
            }

            return false;
        }
    }
}
