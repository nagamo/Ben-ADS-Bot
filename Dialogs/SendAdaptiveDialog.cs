using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ADS.Bot.V1;
using ADS.Bot.V1.Cards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.Sequence;

namespace ADS.Bot1.Dialogs
{
    public class SendAdaptiveDialog<TFactory, TModel> : ComponentDialog where TFactory : ICardFactory<TModel>
    {
        public ADSBotServices Services { get; }
        private ICardFactory<TModel> CardFactory { get; set; }

        public SendAdaptiveDialog(TFactory modelFactory, ADSBotServices services)
            : base(nameof(TFactory))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitStep
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
            Services = services;
            CardFactory = modelFactory;
        }

        private async Task<DialogTurnResult> InitStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await Services.GetUserProfileAsync(stepContext.Context, cancellationToken);
            //TODO: pre-check user details, look if they already have details
            //many more options than just new()'ing at the start
            var initData = await CardFactory.Populate(stepContext.Context, cancellationToken);
            var createdCard = CardFactory.CreateCard(initData, stepContext.Context, cancellationToken);

            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(Utilities.CreateAttachment(createdCard)));

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}