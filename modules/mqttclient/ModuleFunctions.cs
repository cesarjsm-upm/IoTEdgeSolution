namespace mqttclient
{
    using System;
    using System.ComponentModel.DataAnnotations;

    using System.Text.Json;

    public class MqttMessage
    {
        public MqttMessage(string topic, string payload, string contenttype, DateTime timestamp)
        {
            this.topic = topic;
            this.payload = payload;
            this.timestamp = timestamp;
            this.contenttype = contenttype;
        }

        public string topic { get; private set; }
        public string payload { get; private set; }
        public string contenttype { get; private set; }
        public DateTime timestamp { get; private set; }
       

        public override string ToString()
        {
            // return $"Device ID: {deviceId}, Topic: {topic}, Timestamp: {timestamp}, Payload: {payload}";
            return $"Timestamp: {timestamp}, Content-Type: {contenttype}, Topic: {topic}, Payload: {payload}";
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class JsonValidator
    {
        public static bool IsValidJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            input = input.Trim();

            if ((input.StartsWith("{") && input.EndsWith("}")) || // For object
                (input.StartsWith("[") && input.EndsWith("]")))   // For array
            {
                try
                {
                    var jsonElement = JsonDocument.Parse(input).RootElement;
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }
                catch (Exception) // Some other exception
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
