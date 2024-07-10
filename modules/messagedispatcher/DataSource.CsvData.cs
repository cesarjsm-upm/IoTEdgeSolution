namespace messagedispatcher
{
    using System;
    using System.Collections.Generic;

    public class CsvData
    {
        public string Timestamp { get; private set; }
        public string CropType { get; private set; }
        
        public string Topic { get; private set; }
        public string Payload { get; private set; }

        public CsvData(string timestamp, string croptype, string topic, string payload)
        {
            Timestamp = timestamp;
            CropType = croptype;
            Topic = topic;
            Payload = payload;
        }

        public NormalizedData ToNormalizedData()
        {
            // Extracting values from the CSV formatted payload
            Dictionary<string, string> data = new Dictionary<string, string>();
            string[] pairs = Payload.Split(',');

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    data[keyValue[0]] = keyValue[1];
                }
            }

            int idnode = int.Parse(data["idnode"]);
            float temp = float.Parse(data["temp"]);
            float hum = float.Parse(data["hum"]);
            float light = float.Parse(data["light"]);
            float soil = float.Parse(data["soil"]);
            float co2 = float.Parse(data["co2"]);

            // Extract topic parts
            string[] topicParts = Topic.Split('/');
            string topicPart1 = topicParts.Length > 1 ? topicParts[1] : topicParts[0];
            string deviceid = $"{topicParts[0]}{idnode}";

            // Creating a NormalizedData object
            return new NormalizedData(Timestamp, Topic, deviceid, CropType, temp, hum, light, soil, co2, 0 ,0);
        }
    }

}