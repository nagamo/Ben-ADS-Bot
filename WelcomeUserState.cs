namespace ADS.Bot1
{
    // Stores User Welcome state for the conversation.
    // Stored in "Microsoft.Bot.Builder.ConversationState" and
    // backed by "Microsoft.Bot.Builder.MemoryStorage".

    public class WelcomeUserState
    {
        public bool DidBotWelcomeUser { get; set; }
    }
}