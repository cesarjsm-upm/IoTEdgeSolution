namespace messagedispatcher
{
    // using System;
    using System.Text.RegularExpressions;
    //
    // using System;
    // using System.Collections.Generic;
    // using System.Linq;
    using System.Text.Json;
    // using System.Text.RegularExpressions;

    public class DataParser
    {
        public string MyContentType { get; private set; }
        public string MyTimestamp { get; private set; }
        public string MyTopic { get; private set; }
        public string MyPayload { get; private set; }

        // Constructor that takes the input string and parses it
        public DataParser(string input)
        {
            // Regex patterns to capture the sections
            string timestampPattern = @"Timestamp:\s(?<timestamp>.+?),";
            string contentTypePattern = @"Content-Type:\s(?<contentType>.+?),";
            string topicPattern = @"Topic:\s(?<topic>.+?),";
            string payloadPattern = @"Payload:\s(?<payload>.+)$";

            // Match the input string against the patterns
            MyTimestamp = Regex.Match(input, timestampPattern).Groups["timestamp"].Value;
            MyContentType = Regex.Match(input, contentTypePattern).Groups["contentType"].Value;
            MyTopic = Regex.Match(input, topicPattern).Groups["topic"].Value;
            MyPayload = Regex.Match(input, payloadPattern).Groups["payload"].Value;
        }
    }

    public class StringFinder
    {
        // Method to check if the search string is found in the main string
        public bool ContainsString(string mainString, string searchString)
        {
            // Check if either of the strings are null or empty
            if (string.IsNullOrEmpty(mainString) || string.IsNullOrEmpty(searchString))
            {
                return false;
            }

            // Return true if the main string contains the search string, false otherwise
            return mainString.Contains(searchString);
        }
    }

    //  IdNodeExtractor

    public class IdNodeExtractor
    {
        // Method to extract idnode from a comma-separated string
        private int? ExtractFromCommaSeparated(string input)
        {
            var match = Regex.Match(input, @"idnode:(\d+)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return null;
        }

        // Method to extract idnode from a JSON string
        private int? ExtractFromJson(string input)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(input);

                // Check if the JSON string is an object
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // Try to get idnode from the root object
                    if (jsonDoc.RootElement.TryGetProperty("idnode", out var idnodeElement))
                    {
                        return idnodeElement.GetInt32();
                    }

                    // Try to get idnode from nested "objectJSON" field if it exists
                    if (jsonDoc.RootElement.TryGetProperty("objectJSON", out var objectJsonElement))
                    {
                        var nestedJsonDoc = JsonDocument.Parse(objectJsonElement.GetString());
                        if (nestedJsonDoc.RootElement.TryGetProperty("idnode", out var nestedIdnodeElement))
                        {
                            return nestedIdnodeElement.GetInt32();
                        }
                    }
                }

                // Check if the JSON string is an array
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in jsonDoc.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("idnode", out var idnodeElement))
                        {
                            return idnodeElement.GetInt32();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Handle parsing errors if necessary
            }
            return null;
        }

        // Public method to extract idnode from any input format
        public int? ExtractIdNode(string input)
        {
            // Check if input is a comma-separated string
            if (input.Contains("idnode:") && input.Contains(","))
            {
                return ExtractFromCommaSeparated(input);
            }

            // Try to parse input as JSON
            return ExtractFromJson(input);
        }
    }


}