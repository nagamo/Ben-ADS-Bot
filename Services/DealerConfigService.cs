using ADS.Bot.V1.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class DealerConfigService
    {
        public DealerConfigService(IConfiguration configuration)
        {
            Configuration = configuration;

            StorageAccount = CloudStorageAccount.Parse(Configuration.GetConnectionString("DealerConfiguration"));
        }

        Dictionary<string, JObject> DealerConfig = new Dictionary<string, JObject>();
        CloudStorageAccount StorageAccount { get; }
        public IConfiguration Configuration { get; }

        public T Get<T>(UserProfile Profile, string ConfigPath, T FallbackValue = default)
        {
            return GetAsync<T>(Profile, ConfigPath, FallbackValue).Result;
        }

        public async Task<T> GetAsync<T>(UserProfile Profile, string ConfigPath, T FallbackValue = default)
        {
            var dealerID = Profile.Details.DealerID;

            if (string.IsNullOrEmpty(dealerID))
            {
                return FallbackValue;
            }

            if (!DealerConfig.ContainsKey(dealerID) || DealerConfig[dealerID] != null)
            {
                await RefreshDealerAsync(dealerID);
            }

            var config = DealerConfig[dealerID];
            var valueToken = config?.SelectToken(ConfigPath);

            if (valueToken != null)
            {
                return valueToken.Value<T>() ?? FallbackValue;
            }
            else
            {
                return FallbackValue;
            }
        }

        public async Task RefreshDealerAsync(string DealerID)
        {
            var client = StorageAccount.CreateCloudBlobClient();
            try
            {
                var dealerContainer = client.GetContainerReference(Constants.DEALER_CONTAINER);
                await dealerContainer.CreateIfNotExistsAsync();
                var configRef = dealerContainer.GetBlobReference(Path.Join(DealerID, Constants.DEALER_CONFIG_PATH));
                var dealerConfig = await InnerReadBlobAsync(configRef);
                if (dealerConfig != null)
                {
                    DealerConfig[DealerID] = dealerConfig;
                }
                else
                {
                    //Need to init to something...
                    DealerConfig[DealerID] = new JObject();
                }
            }
            catch
            {
                DealerConfig[DealerID] = new JObject();
            }
        }

        private async Task<JObject> InnerReadBlobAsync(CloudBlob blobReference, CancellationToken cancellationToken = default)
        {
            var i = 0;
            while (true)
            {
                try
                {
                    // add request options to retry on timeouts and server errors
                    var options = new BlobRequestOptions { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(20), 4) };

                    using (var blobStream = await blobReference.OpenReadAsync(null, options, new OperationContext(), cancellationToken).ConfigureAwait(false))
                    using (var blobReader = new StreamReader(blobStream))
                    {
                        return JsonConvert.DeserializeObject<JObject>(blobReader.ReadToEnd());
                    }
                }
                catch (StorageException ex)
                    when ((HttpStatusCode)ex.RequestInformation.HttpStatusCode == HttpStatusCode.PreconditionFailed)
                {
                    // additional retry logic, even though this is a read operation blob storage can return 412 if there is contention
                    if (i++ < 8)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public async Task<string> GetQuestionModeAsync(UserProfile Profile, string QuestionPath)
        {
            return (await GetAsync<string>(Profile, $"questions.{QuestionPath}", "enable")).ToLower();
        }

        public async Task<bool> GetAskQuestionAsync(UserProfile Profile, string QuestionPath)
        {
            return await GetQuestionModeAsync(Profile, QuestionPath) != "disable";
        }

        public async Task<bool> IsValidResponseAsync(UserProfile Profile, string Value, string QuestionPath)
        {
            var questionMode = await GetQuestionModeAsync(Profile, QuestionPath);
            if(questionMode == "mandatory")
            {
                return !string.IsNullOrWhiteSpace(Value);
            }
            else
            {
                return true;
            }
        }
    }
}
