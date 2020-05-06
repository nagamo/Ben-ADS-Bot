using ADS.Bot.V1.Models;
using Microsoft.Extensions.Configuration;
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
        }

        private void PopulateCRMLead(UserProfile profile, ZCRMRecord record)
        {
            record.SetFieldValue("First_Name", profile.FirstName);
            record.SetFieldValue("Last_Name", profile.LastName);
            record.SetFieldValue("Email", profile.Details.Email);
            record.SetFieldValue("Phone", profile.Details.Phone);
            record.SetFieldValue("Lead_Source", "Chat");
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

            note.Title = "Financing Details";
            note.Content = string.Join(Environment.NewLine, lines);
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

            note.Title = "Trade-In Details";
            note.Content = details;
        }

        private void PopulateVehicleProfileNote(VehicleInventoryDetails vehicleProfile, ZCRMNote note)
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

            note.Title = "Vehilcle Profile Details";
            note.Content = details;
        }

        private void PopulateNote<T>(ZCRMRecord ParentRecord, T UserData, Action<T, ZCRMNote> PopulateNoteFunction) where T : IADSCRMRecord
        {
            if (UserData != null && UserData.ADS_CRM_ID == null)
            {
                var newExistingNote = UserData.ADS_CRM_ID == null ? new ZCRMNote(ParentRecord) : ZCRMNote.GetInstance(ParentRecord, UserData.ADS_CRM_ID.Value);

                PopulateNoteFunction(UserData, newExistingNote);

                if (UserData.ADS_CRM_ID == null)
                {
                    var createdNote = ParentRecord.AddNote(newExistingNote);
                    if (createdNote.HttpStatusCode != APIConstants.ResponseCode.CREATED) throw new Exception("Failed to create note for lead.");
                    UserData.ADS_CRM_ID = (createdNote.Data as ZCRMNote).Id;
                }
                else
                {
                    var updatedNote = ParentRecord.UpdateNote(newExistingNote);
                    if (updatedNote.HttpStatusCode != APIConstants.ResponseCode.OK) throw new Exception("Failed to update note for lead.");
                }
            }
        }

        public bool RegisterLead(UserProfile profile)
        {
            if (!Connected) return false;
            if (profile.ADS_CRM_ID.HasValue)
                throw new Exception("Profile already has a registered CRM ID.");

            var newLead = new ZCRMRecord("Leads");
            PopulateCRMLead(profile, newLead);
            var createResponse = newLead.Create();

            if(createResponse.HttpStatusCode == APIConstants.ResponseCode.CREATED)
            {
                profile.ADS_CRM_ID = (createResponse.Data as ZCRMRecord).EntityId;
            
                return true;
            }

            return false;
        }

        public bool UpdateLead(UserProfile profile)
        {
            if (!Connected) return false;
            if (!profile.ADS_CRM_ID.HasValue)
                throw new Exception("Profile does not have a registered CRM ID");

            var existingRecord = LeadsModule.GetRecord(profile.ADS_CRM_ID.Value);

            if(existingRecord.HttpStatusCode == APIConstants.ResponseCode.OK)
            {
                var leadRecord = existingRecord.Data as ZCRMRecord;
                
                PopulateCRMLead(profile, leadRecord);
                leadRecord.Update();

                PopulateNote(leadRecord, profile.Financing, PopulateFinancingNote);
                PopulateNote(leadRecord, profile.VehicleProfile, PopulateVehicleProfileNote);
                PopulateNote(leadRecord, profile.TradeDetails, PopulateTradeInNote);

                return true;
            }

            return false;
        }
    }
}
