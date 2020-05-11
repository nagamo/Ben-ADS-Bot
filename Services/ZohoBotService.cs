using ADS.Bot.V1.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ZCRMSDK.CRM.Library.Api;
using ZCRMSDK.CRM.Library.CRMException;
using ZCRMSDK.CRM.Library.CRUD;
using ZCRMSDK.CRM.Library.Setup.RestClient;
using ZCRMSDK.OAuth.Client;

namespace ADS.Bot.V1.Services
{
    public class ZohoBotService
    {
        public bool Connected { get; private set; } = false;

        public ZCRMRestClient ZohoCRMClient { get; private set; }

        private ZCRMModule LeadsModule { get; set; }
        public IConfiguration Configuration { get; }

        public ZohoBotService(IConfiguration configuration)
        {
            try
            {
                ZCRMRestClient.Initialize(new Dictionary<string, string>
                {
                    { "client_id", configuration["zoho:client_id"] },
                    { "client_secret", configuration["zoho:client_secret"] },
                    { "redirect_uri", configuration["zoho:redirect_uri"] },
                    { "currentUserEmail", configuration["zoho:email"] },
                    { "persistence_handler_class","ZCRMSDK.OAuth.ClientApp.ZohoOAuthInMemoryPersistence, ZCRMSDK"} ,
                });
                ZohoOAuthClient.Initialize();

                var authClient = ZohoOAuthClient.GetInstance();
                var tokens = authClient.GenerateAccessTokenFromRefreshToken(configuration["zoho:refresh_token"], configuration["zoho:email"]);

                ZohoCRMClient = ZCRMRestClient.GetInstance();

                LeadsModule = ZohoCRMClient.GetModuleInstance("Leads");
                Connected = true;
            }
            catch (ZCRMException ex)
            {
                Console.WriteLine("Unable to initialize CRM Connection");
            }

            Configuration = configuration;
        }

        private void PopulateCRMLead(UserProfile profile, ZCRMRecord record)
        {
            record.SetFieldValue("First_Name", profile.FirstName);
            //Bit of a hack for people who don't enter a last name.
            record.SetFieldValue("Last_Name", string.IsNullOrWhiteSpace(profile.LastName) ? "-" : profile.LastName);
            if (!string.IsNullOrEmpty(profile.Details.Email)) record.SetFieldValue("Email", profile.Details.Email);
            if (!string.IsNullOrEmpty(profile.Details.Phone)) record.SetFieldValue("Phone", profile.Details.Phone);
            //record.SetFieldValue("Description", $"Focus: {profile.Details.Focus}. Timeframe: {profile.Details.Timeframe}");
            record.SetFieldValue("Lead_Source", "Chat");
        }


        public bool WriteFinancingNote(UserProfile profile)
        {
            return CreateAndPopulateNote(profile, profile.Financing, PopulateFinancingNote);
        }
        private void PopulateFinancingNote(FinancingDetails financing, ZCRMNote note)
        {
            var lines = financing.GoodCredit ?
                new string[]
                {
                    $"Credit Score: {financing.CreditScore}"
                } :
                new string[]
                {
                    $"Credit Score: {financing.CreditScore}",
                    $"Income: {financing.Income}",
                    $"Home Ownership: {financing.HomeOwnership}",
                    $"Employment History: {financing.Employment}",
                };

            note.Title = $"Financing Details {DateTime.Now:g}";
            note.Content = string.Join(Environment.NewLine, lines);
        }


        public bool WriteTradeInNote(UserProfile profile)
        {
            return CreateAndPopulateNote(profile, profile.TradeDetails, PopulateTradeInNote);
        }
        private void PopulateTradeInNote(TradeInDetails tradein, ZCRMNote note)
        {
            string details = string.Join(Environment.NewLine, new string[]
            {
                $"Make: {tradein.Make}",
                $"Model: {tradein.Model}",
                $"Year: {tradein.Year}",
                $"Mileage: {tradein.Mileage}",
                $"Condition: {tradein.Condition}",
                $"Amount Owed: {tradein.AmountOwed}",
            });

            note.Title = $"Trade-In Details {DateTime.Now:g}";
            note.Content = details;
        }


        public bool WriteInventoryNote(UserProfile profile)
        {
            return CreateAndPopulateNote(profile, profile.Inventory, PopulateInventoryNote);
        }
        private void PopulateInventoryNote(VehicleInventoryDetails inventory, ZCRMNote note)
        {
            string details = string.Join(Environment.NewLine, new string[]
            {
                $"Primary Concern: {inventory.PrimaryConcern}",
                $"Goal: {inventory.ConcernGoal}"
            });

            note.Title = $"Inventory Inquiry {DateTime.Now:g}";
            note.Content = details;
        }


        public bool WriteVehicleProfileNote(UserProfile profile)
        {
            return CreateAndPopulateNote(profile, profile.VehicleProfile, PopulateVehicleProfileNote);
        }
        private void PopulateVehicleProfileNote(VehicleProfileDetails vehicleProfile, ZCRMNote note)
        {
            string details = string.Join(Environment.NewLine, new string[]
            {
                $"Needs Financing: {((vehicleProfile.NeedFinancing ?? false) ? "Yes" : "No")}",
                $"Trading-In: {((vehicleProfile.TradingIn ?? false) ? "Yes" : "No")}",
                $"",
                $"Make: {vehicleProfile.Make}",
                $"Model: {vehicleProfile.Model}",
                $"Year: {vehicleProfile.Year}",
                $"Color: {vehicleProfile.Color}"
            });

            note.Title = $"Vehilcle Profile Details {DateTime.Now:g}";
            note.Content = details;
        }

        private bool CreateAndPopulateNote<T>(UserProfile profile, T UserData, Action<T, ZCRMNote> PopulateNoteFunction) where T : IADSCRMRecord
        {
            if (!Connected) return false;
            if (!profile.ADS_CRM_ID.HasValue)
                throw new Exception("Profile does not have a registered CRM ID");
            
            var leadRecord = ZCRMRecord.GetInstance("Leads", profile.ADS_CRM_ID);

            if (UserData != null && UserData.ADS_CRM_ID == null)
            {
                var newExistingNote = UserData.ADS_CRM_ID == null ? new ZCRMNote(leadRecord) : ZCRMNote.GetInstance(leadRecord, UserData.ADS_CRM_ID.Value);

                PopulateNoteFunction(UserData, newExistingNote);

                if (UserData.ADS_CRM_ID == null)
                {
                    var createdNote = leadRecord.AddNote(newExistingNote);
                    if (createdNote.HttpStatusCode != APIConstants.ResponseCode.CREATED) throw new Exception("Failed to create note for lead.");
                    UserData.ADS_CRM_ID = (createdNote.Data as ZCRMNote).Id;
                }
                else
                {
                    var updatedNote = leadRecord.UpdateNote(newExistingNote);
                    if (updatedNote.HttpStatusCode != APIConstants.ResponseCode.OK) throw new Exception("Failed to update note for lead.");
                }
            }

            return false;
        }

        private bool RegisterLead(UserProfile profile)
        {
            if (!Connected) return false;
            if (profile.ADS_CRM_ID.HasValue)
                throw new Exception("Profile already has a registered CRM ID.");

            var newLead = new ZCRMRecord("Leads");
            PopulateCRMLead(profile, newLead);
            try
            {
                var createResponse = newLead.Create();

                if(createResponse.HttpStatusCode == APIConstants.ResponseCode.CREATED)
                {
                    profile.ADS_CRM_ID = (createResponse.Data as ZCRMRecord).EntityId;
            
                    return true;
                }
            }
            catch (ZCRMException ex)
            {
                throw new Exception($"ZOHO Error: {ex.Message} ({JsonConvert.SerializeObject(ex.Data)})", ex);
            }

            return false;
        }

        public long CreateUpdateLead(UserProfile profile)
        {
            if (profile.ADS_CRM_ID.HasValue) return profile.ADS_CRM_ID.Value;

            if (RegisterLead(profile))
                return profile.ADS_CRM_ID.Value;
            else
                throw new Exception("Error creating lead in CRM system.");
        }
    }
}
