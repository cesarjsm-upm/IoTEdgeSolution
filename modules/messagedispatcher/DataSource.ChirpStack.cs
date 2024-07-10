namespace messagedispatcher
{
    using System;
    using Newtonsoft.Json.Linq;

    public class ChirpStackData
    {
        public string Timestamp { get; private set; }
        public string CropType { get; private set; }
        
        public string Topic { get; private set; }
        public string Payload { get; private set; }

        public ChirpStackData(string timestamp, string croptype, string topic, string payload)
        {
            Timestamp = timestamp;
            CropType = croptype;
            Topic = topic;
            Payload = payload;
        }

        public NormalizedData ToNormalizedData()
        {
            // Extracting the objectJSON section from the payload
            JObject payloadJson = JObject.Parse(Payload);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            string objectJsonString = payloadJson["objectJSON"].ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            JObject objectJson = JObject.Parse(objectJsonString);

#pragma warning disable CS8604 // Possible null reference argument.
            int idnode = objectJson["idnode"].Value<int>();
            float temp = objectJson["temp"].Value<float>();
            float hum = objectJson["hum"].Value<float>();
            float light = objectJson["light"].Value<float>();
            float soil = objectJson["soil"].Value<float>();
            float co2 = objectJson["co2"].Value<float>();
            float soilph = objectJson["soilph"].Value<float>();
            float solarradiation = objectJson["solarradiation"].Value<float>();
#pragma warning restore CS8604 // Possible null reference argument.
            
            // Extract topic value until the first "/"
            // string normalizedTopic = Topic.Split('/')[1];
            string normalizedTopic = Topic;

            // Concatenate CHIRP and idnode to form the deviceid
            // string strNode = "CHIRP";
            // string deviceid = $"{strNode}{idnode}";
            string deviceid = $"{idnode}";

            // Creating a NormalizedData object
            return new NormalizedData(Timestamp, normalizedTopic, deviceid, CropType, temp, hum, light, soil, co2, soilph, solarradiation);
        }
    }

}