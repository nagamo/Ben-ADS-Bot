using AdaptiveCards;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Cards
{
    public abstract class JSONCardFactory<TModel> : ICardFactory<TModel>
    {
        static AdaptiveTransformer Transformer = new AdaptiveTransformer();

        string JSON = null;

        public JSONCardFactory(string id, string jsonPath)
        {
            if (jsonPath is null)
            {
                throw new ArgumentNullException(nameof(jsonPath));
            }

            Id = id ?? throw new ArgumentNullException(nameof(id));

            JSON = File.ReadAllText(jsonPath);
        }

        public string Id { get; private set; }

        public AdaptiveCard CreateCard(TModel startingData, ITurnContext context, CancellationToken cancellationToken = default)
        {
            var contextJSON = JsonConvert.SerializeObject(startingData);
            //Transform our template with the data model specified
            var transformedJSON = Transformer.Transform(JSON, contextJSON);
            //And then create the card from it
            AdaptiveCardParseResult parseResult = AdaptiveCard.FromJson(transformedJSON);
            
            //Print any warnings
            //TODO: This should tap into app insights stream somewhere
            if (parseResult.Warnings.Any())
            {
                foreach(var warning in parseResult.Warnings)
                {
                    Console.WriteLine($"[WARN] {warning.Code} - {warning.Message}");
                }
            }

            return parseResult.Card;
        }



        internal abstract Task<TModel> DoPopulate(ITurnContext context, CancellationToken cancellationToken);
        internal abstract Task<bool> DoValidate(TModel submission, ITurnContext context, CancellationToken cancellationToken);
        internal abstract Task DoFinalize(TModel submission, ITurnContext context, CancellationToken cancellationToken);


        async Task<TModel> ICardFactory<TModel>.Populate(ITurnContext context, CancellationToken cancellationToken = default)
        {
            return await DoPopulate(context, cancellationToken);
        }
        async Task<bool> ICardFactory<TModel>.Validate(TModel submission, ITurnContext context, CancellationToken cancellationToken)
        {
            return await DoValidate(submission, context, cancellationToken);
        }

        async Task ICardFactory<TModel>.Finalize(TModel submission, ITurnContext context, CancellationToken cancellationToken)
        {
            await DoFinalize(submission, context, cancellationToken);
        }
    }
}
