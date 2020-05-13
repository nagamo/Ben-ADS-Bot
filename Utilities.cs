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

        public static PromptOptions CreateOptions(IEnumerable<string> Options, string PromptText, string RetryText = null)
        {
            return CreateOptions(Options.ToArray(), MessageFactory.Text(PromptText), MessageFactory.Text(RetryText));
        }

        public static PromptOptions CreateOptions(IEnumerable<string> Options, Activity Prompt, Activity Retry = null)
        {
            var promptOpts = new PromptOptions();

            promptOpts.Choices = Options.Select(opt =>
            {
                return new Choice(opt);
            }).ToList();

            promptOpts.Prompt = Prompt;
            promptOpts.RetryPrompt = Retry;

            return promptOpts;
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

        public static IMessageActivity CreateTestCarousel(ITurnContext context)
        {
            var reply = context.Activity.CreateReply();

            var attachment = new
            {
                type = "template",
                payload = new
                {
                    template_type = "generic",
                    elements = new[]
                        {
                        new {
                            title = "Three Strategies for Finding Snow",
                            image_url = "https://static01.nyt.com/images/2019/02/10/travel/03update-snowfall2/03update-snowfall2-jumbo.jpg?quality=90&auto=webp",
                            subtitle = "How do you plan a ski trip to ensure the best conditions? You can think about a resort’s track record, or which have the best snow-making machines. Or you can gamble.",
                            default_action = new {
                                type = "web_url",
                                url = "https://www.nytimes.com/2019/02/08/travel/ski-resort-snow-conditions.html",
                            },
                            buttons = new object[]
                            {
                                new {
                                    type = "web_url",
                                    url = "https://www.nytimes.com/2019/02/08/travel/ski-resort-snow-conditions.html",
                                    title = "View Article"
                                },
                                new {
                                    type = "element_share"
                                },
                            },
                        },new {
                            title = "Viewing the Northern Lights: ‘It’s Almost Like Heavenly Visual Music’",
                            image_url = "https://static01.nyt.com/images/2019/02/17/travel/17Northern-Lights1/17Northern-Lights1-superJumbo.jpg?quality=90&auto=webp",
                            subtitle = "Seeing the aurora borealis has become a must-do item for camera-toting tourists from Alaska to Greenland to Scandinavia. On a trip to northern Sweden, the sight proved elusive, if ultimately rewarding.",
                            default_action = new {
                                type = "web_url",
                                url = "https://www.nytimes.com/2019/02/08/travel/ski-resort-snow-conditions.html",
                            },
                            buttons = new object[]
                            {
                                new {
                                    type = "web_url",
                                    url = "https://www.nytimes.com/2019/02/11/travel/northern-lights-tourism-in-sweden.html",
                                    title = "View Article"
                                },
                                new {
                                    type = "element_share"
                                },
                            },
                        },new {
                            title = "Five Places to Visit in New Orleans",
                            image_url = "https://static01.nyt.com/images/2019/02/10/travel/03update-snowfall2/03update-snowfall2-jumbo.jpg?quality=90&auto=webp",
                            subtitle = "Big Freedia’s rap music is a part of the ether of modern New Orleans. So what better authentic travel guide to the city that so many tourists love to visit?",
                            default_action = new {
                                type = "web_url",
                                url = "https://static01.nyt.com/images/2019/02/17/travel/17NewOrleans-5Places6/17NewOrleans-5Places6-jumbo.jpg?quality=90&auto=webp",
                            },
                            buttons = new object[]
                            {
                                new {
                                    type = "web_url",
                                    url = "https://static01.nyt.com/images/2019/02/17/travel/17NewOrleans-5Places6/17NewOrleans-5Places6-jumbo.jpg?quality=90&auto=webp",
                                    title = "View Article"
                                },
                                new {
                                    type = "element_share"
                                },
                            },
                        },
                    },
                },
            };

            reply.ChannelData = JObject.FromObject(new { attachment });

            return reply;
        }
    }
}
