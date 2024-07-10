namespace messagedispatcher
{

    using Newtonsoft.Json;
    // using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class IoTDevice
    {
        [JsonProperty("IdNode")]
        public string? IdNode { get; set; }

        [JsonProperty("Device")]
        public string? Device { get; set; }

        [JsonProperty("Brand")]
        public string? Brand { get; set; }

        [JsonProperty("Gateway")]
        public string? Gateway { get; set; }

         [JsonProperty("GatewayID")]
        public string? GatewayID { get; set; }

        [JsonProperty("IsGateway")]
        public bool IsGateway { get; set; }

        [JsonProperty("Protocol")]
        public string? Protocol { get; set; }

        [JsonProperty("Datasource")]
        public string? Datasource { get; set; }

        [JsonProperty("Sendtomonitor")]
        public bool SendToMonitor { get; set; }
                
        [JsonProperty("Crop")]
        public string? Crop { get; set; }
    }


    public class IoTDeviceManager
    {
        private List<IoTDevice> devices;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public IoTDeviceManager(string filePath)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            // Cargar el JSON desde el archivo
            string json = File.ReadAllText(filePath);
#pragma warning disable CS8601 // Possible null reference assignment.
            devices = JsonConvert.DeserializeObject<List<IoTDevice>>(json);
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        public IoTDevice GetDeviceById(string idNode)
        {
            // Buscar el dispositivo por IdNode
#pragma warning disable CS8603 // Possible null reference return.
            return devices.FirstOrDefault(d => d.IdNode == idNode);
#pragma warning restore CS8603 // Possible null reference return.
        }
    }


}
