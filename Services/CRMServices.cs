using ADS.Bot.V1.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class CRMService
    {
        public CRMService(IConfiguration configuration, 
            ZohoAPIService zohoService, BuyerBridgeAPIService bbService)
        {
            Configuration = configuration;
            ZohoService = zohoService;
            BBService = bbService;
            IsActive = false;

            CheckEnabled();
        }

        public IConfiguration Configuration { get; }
        public ZohoAPIService ZohoService { get; }
        public BuyerBridgeAPIService BBService { get; }
        public bool IsActive { get; private set; }

        private void CheckEnabled()
        {
            if (Configuration.GetValue<bool>("ads:crm_enabled"))
            {
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
                    //ZohoService.CreateUpdateLead(profile);
                    break;
                case CRMStage.FinancingCompleted:
                    BBService.CreateUpdateLead(profile);
                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteFinancingNote(profile);
                    break;
                case CRMStage.VehicleProfileCompleted:
                    BBService.CreateUpdateLead(profile);
                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteVehicleProfileNote(profile);
                    break;
                case CRMStage.ValueTradeInCompleted:
                    BBService.CreateUpdateLead(profile);
                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteTradeInNote(profile);
                    break;
                case CRMStage.VehicleInventoryCompleted:
                    BBService.CreateUpdateLead(profile);
                    ZohoService.CreateUpdateLead(profile);
                    ZohoService.WriteInventoryNote(profile);
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
        VehicleInventoryCompleted,
    }
}
