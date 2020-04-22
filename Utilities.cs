using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
