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
        public int? Year { get; set; }
        public string Color { get; set; }

        public double? Price { get; set; }

        public string ImageURL { get; set; }
        public string URL { get; set; }
        public string VIN => RowKey;
    }
}
