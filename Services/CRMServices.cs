using ADS.Bot.V1.Models;
using ADS.Bot1;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class CRMService
    {
        internal ADSBotServices Services { get; set; }

        public CRMService(IConfiguration configuration, 
            ZohoAPIService zohoService, BuyerBridgeAPIService bbService, DealerConfigService dealerConfig)
        {
            Configuration = configuration;
            ZohoService = zohoService;
            BBService = bbService;
            DealerConfig = dealerConfig;
            IsActive = false;

            BBService.RootCRM = this;

            CheckEnabled();
        }

        public IConfiguration Configuration { get; }
        public ZohoAPIService ZohoService { get; }
        public BuyerBridgeAPIService BBService { get; }
        public DealerConfigService DealerConfig { get; }
        public bool IsActive { get; private set; }

        private void CheckEnabled()
        {
            if (Configuration.GetValue<bool?>("ads:crm_enabled") ?? true)
            {
                if(!ZohoService.Error && !ZohoService.Connected)
                {
                    ZohoService.Connect();
                }

                if (ZohoService.Connected)
                {
                    IsActive = true;
                }
            }
        }

        public void WriteCRMDetails(CRMStage currentStage, UserProfile profile)
        {
            CheckEnabled();
            if (!IsActive) return;

            switch (currentStage)
            {
                case CRMStage.New:
                    break;
                case CRMStage.BasicDetails:
                    ZohoService.CreateUpdateLead(profile);
                    Services.AI_Event("Profile_Completed", profile);

                    break;
                case CRMStage.FinancingCompleted:
                    Services.AI_Event("Finished_Financing", profile);

                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteFinancingNote(profile);
                    break;
                case CRMStage.VehicleProfileCompleted:
                    Services.AI_Event("Finished_Vehicle_Profile", profile);

                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteVehicleProfileNote(profile);
                    break;
                case CRMStage.ValueTradeInCompleted:
                    Services.AI_Event("Finished_Tradein", profile);

                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteTradeInNote(profile);
                    break;
                case CRMStage.SimpleInventoryCompleted:
                    Services.AI_Event("Finished_Inventory", profile);

                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteInventoryNote(profile);
                    break;
                case CRMStage.Fnalize:
                    try
                    {
                        bool financeValid = profile.Financing?.IsValidForSubmit(profile, Services).Result ?? false;
                        bool tradeinValid = profile.TradeDetails?.IsCompleted ?? false;
                        bool inventoryValid = profile.SimpleInventory?.IsCompleted ?? false;

                        //If user hasn't completed any forms yet (they stopped chatting after initial questions)
                        //Don't write them out to BB
                        if (!financeValid && !tradeinValid && !inventoryValid)
                            return;

                        bool allowResubmit = DealerConfig.Get<bool>(profile, "repeat_lead", false);

                        string userUniqueID = profile.Details.UniqueID;

                        if (!string.IsNullOrEmpty(profile.BB_CRM_ID))
                        {
                            if (allowResubmit)
                            {
                                userUniqueID = $"{userUniqueID}_{DateTime.Now:s}";

                                Services.AI_Event("BB_Resubmit", profile);
                            }
                            else
                            {
                                Services.AI_Event("BB_Skipping", profile);
                                return;
                            }
                        }
                        else
                        {
                            Services.AI_Event("BB_Submit", profile);
                        }

                        BBService.CreateUpdateLead(profile, userUniqueID);
                    }
                    catch (Exception ex)
                    {
                        Services.AI_Event("BB_Failed", profile);
                        Services.AI_Exception(ex, profile, "Error while finalizing.");
                    }
                    break;
            }
        }
    }

    public enum CRMStage
    {
        New,
        BasicDetails,
        FinancingCompleted,
        VehicleProfileCompleted,
        ValueTradeInCompleted,
        SimpleInventoryCompleted,
        Fnalize
    }
}
