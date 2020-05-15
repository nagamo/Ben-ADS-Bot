using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    public class DB_Car : TableEntity
    {
        public string VIN => RowKey;

        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string Color { get; set; }
        public string Display_Name { get; set; }
        public string Stock_Number { get; set; }
        public int Price { get; set; }
        public int Mileage { get; set; }
        public string Engine { get; set; }
        public string Transmission { get; set; }
        public string Doors { get; set; }
        public bool Used { get; set; }
        
        public string URL { get; set; }
        public string Image_URL { get; set; }
    }
}
