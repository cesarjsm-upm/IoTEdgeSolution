namespace edgemonitor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;


    // ****** CROP MONITOR ******
    public class Range
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }

    public class CropData
    {
        public Range? Temperature { get; set; }
        public Range? SoilMoisture { get; set; }
        public Range? SoilPh { get; set; }
        public Range? Humidity { get; set; }
        public Range? Light { get; set; }
        public Range? SolarRadiation { get; set; }
        public Range? Co2 { get; set; }
    }

    public class CropMonitor
    {
        private Dictionary<string, CropData> cropsData;

        public CropMonitor(string jsonFilePath)
        {
            string jsonData = File.ReadAllText(jsonFilePath);
            cropsData = JsonConvert.DeserializeObject<Dictionary<string, CropData>>(jsonData);
        }

        public bool IsMeasureWithinRange(string cropName, string measureName, double measureValue)
        {
            if (!cropsData.ContainsKey(cropName))
            {
                throw new ArgumentException($"Crop {cropName} not found.");
            }

            var cropData = cropsData[cropName];
            var measure = cropData.GetType().GetProperty(measureName)?.GetValue(cropData) as Range;

            if (measure == null)
            {
                throw new ArgumentException($"Measure {measureName} not found.");
            }

            return measureValue >= measure.Min && measureValue <= measure.Max;
        }

        public Range GetMeasureRange(string cropName, string measureName)
        {
            if (!cropsData.ContainsKey(cropName))
            {
                throw new ArgumentException($"Crop {cropName} not found.");
            }

            var cropData = cropsData[cropName];
            var measure = cropData.GetType().GetProperty(measureName)?.GetValue(cropData) as Range;

            if (measure == null)
            {
                throw new ArgumentException($"Measure {measureName} not found.");
            }

            return measure;
        }
    }

    // ***** TELEMETRY-DATA *****

    public class TelemetryData
    {
        public string? Timestamp { get; set; }
        public string? Topic { get; set; }
        public string? DeviceID { get; set; }
        public string? CropType { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Light { get; set; }
        public double SoilMoisture { get; set; }
        public double Co2 { get; set; }
        public double SoilPh { get; set; }
        public double SolarRadiation { get; set; }
    }

    public class JsonProcessor
    {
        public static TelemetryData DeserializeJsonToObject(string jsonString)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return JsonConvert.DeserializeObject<TelemetryData>(jsonString);
#pragma warning restore CS8603 // Possible null reference return.
        }
    }

}