using System;
using ADS.Bot1.Bots;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace ADS.Bot1
{
    public class BotAccessors
    {
        public BotAccessors(ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(DialogStateAccessorName);
        }

        public static string DialogStateAccessorName { get; } = $"{nameof(BotAccessors)}.DialogState";
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; internal set; }
        public ConversationState ConversationState { get; }
    }
}