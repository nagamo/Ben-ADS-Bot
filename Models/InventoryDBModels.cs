using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    public class DB_Car : TableEntity
    {
        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string Color { get; set; }
        public string Color_Raw { get; set; }
        public string Body { get; set; }
        public string Display_Name { get; set; }
        public string Stock_Number { get; set; }
        public int Price { get; set; }
        public int Mileage { get; set; }
        public string Engine { get; set; }
        public string Transmission { get; set; }
        public string Doors { get; set; }
        public bool Used { get; set; }
        public string Description { get; set; }
        public string Features { get; set; }

        public string URL { get; set; }
        public string Image_URL { get; set; }


        //This done as a function because TableQuery will not handle it properly
        //if it's just an alias around RowKey property
        public string VIN()
        {
            return RowKey;
        }
    }

    public class DB_Dealer : TableEntity
    {
        //ParitionKey is Empty
        //RowKey is ID
        public string Agency_ID { get; set; }
        public string Name { get; set; }
        public string Site_Provider_ID { get; set; }
        public string Site_Provider { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string Country_Code { get; set; }
        public string FB_PageIDs { get; set; }
    }
}
