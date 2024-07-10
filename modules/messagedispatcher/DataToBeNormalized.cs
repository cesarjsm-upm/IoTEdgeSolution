namespace messagedispatcher
{
    // using System;
    using Newtonsoft.Json;

    public class NormalizedData
    {
        public string Timestamp { get; private set; }
        public string Topic { get; private set; }
        public string DeviceID { get; private set; }
        public string CropType { get; private set; }
        public float Temperature { get; private set; }
        public float Humidity { get; private set; }
        public float Light { get; private set; }
        public float SoilMoisture { get; private set; }
        public float Co2 { get; private set; }
        public float SoilPh { get; private set; }
        public float SolarRadiation { get; private set; }

        public NormalizedData(string timestamp, string topic, string deviceid, string croptype, float temp, float hum, 
                              float light, float soil, float co2, float soilPh, float solarRadiation)
        {
            Timestamp = timestamp;
            Topic = topic;
            DeviceID = deviceid;
            CropType =  croptype;
            // Measurements
            Temperature = (float)Math.Round(temp, 2);
            Humidity = (float)Math.Round(hum, 2);
            Light = (float)Math.Round(light, 2);
            SoilMoisture = (float)Math.Round(soil, 2);
            Co2 = (float)Math.Round(co2, 2);
            SoilPh = (float)Math.Round(soilPh, 2);
            SolarRadiation = (float)Math.Round(solarRadiation, 2);
            //
        }

        public override string ToString()
        {
            // Format the parts of the string and assign it to a variable
            string part1 = $"Timestamp: {Timestamp}, Topic: {Topic}, DeviceID: {DeviceID}, CropType: {CropType}, Temperature: {Temperature:F2}, ";
            string part2 = $"Humidity: {Humidity:F2}, Light: {Light:F2}, SoilMoisture: {SoilMoisture:F2}, Co2: {Co2:F2}";
            string part3 = $"SoilPh: {SoilPh:F2}, SolarRadiation: {SolarRadiation:F2}";

            // Concatenate the parts
            string formattedData = part1 + part2 + part3;

            // Return the concatenated string
            return formattedData;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

}



