using AdaptiveCards;
using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Cards
{
    public interface ICardFactory
    {
        public string Id { get; }
        public virtual async Task<bool> OnValidateCard(JObject data, ITurnContext context, CancellationToken cancellationToken = default) { return true; }
        public virtual async Task OnFinalizeCard(JObject data, ITurnContext context, CancellationToken cancellationToken = default) { }
    }

    public interface ICardFactory<TModel> : ICardFactory
    {
        public AdaptiveCard CreateCard(TModel startingData, ITurnContext context, CancellationToken cancellationToken = default);
        virtual public Task<TModel> Populate(ITurnContext context, CancellationToken cancellationToken = default) { return default; }



        virtual internal async Task<bool> Validate(TModel submission, ITurnContext context, CancellationToken cancellationToken = default) { return true; }
        virtual internal async Task Finalize(TModel submission, ITurnContext context, CancellationToken cancellationToken = default) { }



        async Task<bool> ICardFactory.OnValidateCard(JObject data, ITurnContext context, CancellationToken cancellationToken = default)
        {
            return await this.Validate(data.ToObject<TModel>(), context, cancellationToken);
        }
        
        async Task ICardFactory.OnFinalizeCard(JObject data, ITurnContext context, CancellationToken cancellationToken = default)
        {
            await this.Finalize(data.ToObject<TModel>(), context, cancellationToken);
        }
    }
}
