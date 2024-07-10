namespace messagedispatcher
{
    using System;
    using Newtonsoft.Json.Linq;

    public class NodeREDData
    {
       
        public string Timestamp { get; private set; }
        public string CropType { get; private set; }
        
        public string Topic { get; private set; }
        public string Payload { get; private set; }

        public NodeREDData(string timestamp, string croptype, string topic, string payload)
        {
            Timestamp = timestamp;
            CropType = croptype;
            Topic = topic;
            Payload = payload;
        }

        public NormalizedData ToNormalizedData()
        {
            // Extracting the first item in the JSON array payload
            JArray payloadArray = JArray.Parse(Payload);
            JObject objectJson = (JObject)payloadArray[0];

#pragma warning disable CS8604 // Possible null reference argument.
            int idnode = objectJson["idnode"].Value<int>();
            float temperature = objectJson["temperature"].Value<float>();
            float humidity = objectJson["humidity"].Value<float>();
            float light = objectJson["light"].Value<float>();
            float co2 = objectJson["co2"].Value<float>();
            float soilmoisture = objectJson["soilmoisture"].Value<float>();
#pragma warning restore CS8604 // Possible null reference argument.
            
            // Extract topic parts
            string[] topicParts = Topic.Split('/');
            string topicPart1 = topicParts.Length > 1 ? topicParts[1] : topicParts[0];
            // string deviceid = $"{topicParts[0]}{idnode}";
            string deviceid = $"{idnode}";

            // Creating a NormalizedData object
            return new NormalizedData(Timestamp, Topic, deviceid, CropType, temperature, humidity, light, soilmoisture, co2, 0, 0);
        }
    }

}