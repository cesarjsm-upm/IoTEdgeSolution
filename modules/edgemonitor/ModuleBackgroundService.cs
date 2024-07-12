using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Text;
using Microsoft.Azure.Devices.Shared; // For TwinCollection
using Newtonsoft.Json;                // For JsonConvert

using MQTTnet;
using MQTTnet.Client;
// using MQTTnet.Client.Options;

namespace edgemonitor;

internal class ModuleBackgroundService : BackgroundService
{
    private int _counter;
    private ModuleClient? _moduleClient;

    private CancellationToken _cancellationToken;
    private readonly ILogger<ModuleBackgroundService> _logger;

    public ModuleBackgroundService(ILogger<ModuleBackgroundService> logger) => _logger = logger;

    // TelemetryInterval
    static int telemetryInterval { get; set; } = 5;

    // ************* MQTT CLIENT *************************
    private IMqttClient? _mqttClient;
    private string mqttclientAddress = "192.168.0.200";
    private string mqttPubTopic = "edgemonitor/datanormalized";
    private string mqttPubTopicAlert = "edgemonitor/alert";
    private bool mqttConected = false;

    // 
    private CropMonitor cropMonitor = new CropMonitor("/app/data/crop_parameters.json");

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        MqttTransportSettings mqttSetting = new(TransportType.Mqtt_Tcp_Only);
        ITransportSettings[] settings = { mqttSetting };

        // Open a connection to the Edge runtime
        _moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);

        // Reconnect is not implented because we'll let docker restart the process when the connection is lost
        _moduleClient.SetConnectionStatusChangesHandler((status, reason) =>
            _logger.LogWarning("Connection changed: Status: {status} Reason: {reason}", status, reason));

        await _moduleClient.OpenAsync(cancellationToken);

        _logger.LogInformation("EDGE MONITOR module client initialized.");

        // Initialize MQTT Client
        await InitializeMqttClientAsync();

        // Initialize CROP MONITOR
        // await InitializeCropMonitorAsync();
        // cropMonitor = new CropMonitor("/app/data/crop_parameters.json");

        // Read values from the module twin's desired properties
        var moduleTwin = await _moduleClient.GetTwinAsync();
        await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, _moduleClient);

        // Attach a callback for updates to the module twin's desired properties.
        await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

        // Register callback to be called when a message is received by the module
        await _moduleClient.SetInputMessageHandlerAsync("inputFromMessageDispatcher", ProcessMessageAsync, null, cancellationToken);
    }

    private async Task InitializeMqttClientAsync()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithClientId("edgemonitor")
                    .WithTcpServer(mqttclientAddress, 1883) // Replace with your broker's address and port
                    .WithCleanSession()
                    .Build();

        var connectResult = await _mqttClient.ConnectAsync(mqttClientOptions, _cancellationToken);

        if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
        {
            _logger.LogInformation("Connected to MQTT BROKER: {mqttclientAddress}", mqttclientAddress);
            mqttConected = true;
        }
        else
        {
            _logger.LogWarning("Failed to connect to MQTT BROKER: {mqttclientAddress}", mqttclientAddress);
        }
    }


    async Task<MessageResponse> ProcessMessageAsync(Message message, object userContext)
    {
        try
        {
            int counterValue = Interlocked.Increment(ref _counter);

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            _logger.LogInformation("Received message: {counterValue}, Body: [{messageString}]", counterValue, messageString);


            if (!string.IsNullOrEmpty(messageString))
            {
                // ************************ SEND TELEMETRY TO IOT HUB ************  
                using Message pipeMessage = new(messageBytes);
                foreach (KeyValuePair<string, string> prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);
                }
                await _moduleClient!.SendEventAsync("EdgeIoTHub", pipeMessage, _cancellationToken);

                _logger.LogInformation("Message SENT to IoT Hub: {messageString}", messageString);

                // ************************ SEND TO MQTT BROKER ******************
                if (mqttConected && _mqttClient != null)
                {
                    var messageToMQTT = new MqttApplicationMessageBuilder()
                     .WithTopic(mqttPubTopic)
                     .WithPayload(messageString)
                     .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                     .WithRetainFlag(false)
                     .Build();

                    // Publish the message  
                    await _mqttClient.PublishAsync(messageToMQTT);
                    _logger.LogInformation("Message SENT to MQTT BROKER: Topic: {mqttPubTopic} Payload: {messageString}", mqttPubTopic, messageString);
                };
                // ************************ SEND TO MQTT BROKER ******************

                //********* VERIFY IF MEASUREMENTS IS OUT OF THRESHOLDS **********
                TelemetryData telemetryData = JsonProcessor.DeserializeJsonToObject(messageString);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string cropName = telemetryData.CropType;
                string measureName = "";
                double measureValue = 0;
                string alertPayload = "";
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                //Console.WriteLine("Timestamp: " + telemetryData.Timestamp);
                //Console.WriteLine("Topic: " + telemetryData.Topic);
                //Console.WriteLine("DeviceID: " + telemetryData.DeviceID);   

                for (int i = 1; i < 8; i++)
                {
                    switch (i)
                    {
                        case 1:
                            measureName = "Temperature";
                            measureValue = telemetryData.Temperature;
                            break;
                        case 2:
                            measureName = "Humidity";
                            measureValue = telemetryData.Humidity;
                            break;
                        case 3:
                            measureName = "Light";
                            measureValue = telemetryData.Light;
                            break;
                        case 4:
                            measureName = "SoilMoisture";
                            measureValue = telemetryData.SoilMoisture;
                            break;
                        case 5:
                            measureName = "Co2";
                            measureValue = telemetryData.Co2;
                            break;
                        case 6:
                            measureName = "SoilPh";
                            measureValue = telemetryData.SoilPh;
                            break;
                        case 7:
                            measureName = "SolarRadiation";
                            measureValue = telemetryData.SolarRadiation;
                            break;
                        default:
                            break;
                    }

                    // **** SEND ALERT TO MQTT
#pragma warning disable CS8604 // Possible null reference argument.
                    bool isWithinRange = cropMonitor.IsMeasureWithinRange(cropName, measureName, measureValue);
#pragma warning restore CS8604 // Possible null reference argument.

                    Range range = cropMonitor.GetMeasureRange(cropName, measureName);
                    if (!isWithinRange)
                    {
                        // _logger.LogInformation($"OUT OF THRESHOLDS: {measureName} value = {measureValue} range for {cropName}: Min = {range.Min}, Max = {range.Max}");
                        //string alertPayload = $"DeviceID: {telemetryData.DeviceID}, CropType: {cropName}, {measureName}: {measureValue}, RangeMin: {range.Min}, RangeMax: {range.Max}";
                        if (!string.IsNullOrEmpty(alertPayload))
                        {
                            alertPayload = alertPayload + ",";
                        }
                        // alertPayload = alertPayload + $"\"{measureName}\": {measureValue}, \"RangeMin\": {range.Min}, \"RangeMax\": {range.Max}";
                        alertPayload = alertPayload + $"\"{measureName}\": {measureValue}";

                    }
                    //Console.WriteLine($"{measureName} for {cropName} is within range: {isWithinRange}");
                    //Console.WriteLine($"{measureName} range for {cropName}: Min = {range.Min}, Max = {range.Max}");
                    //********* VERIFY IF MEASUREMENTS IS OUT OF THRESHOLDS **********
                } // for i


                // there is an alert to send
                if (!string.IsNullOrEmpty(alertPayload))
                {
                    var alertTimestamp = GetTimestamp();

                    // var startPayload = $"\"Timestamp\": {alertTimestamp}, \"DeviceID\": {telemetryData.DeviceID}, \"CropType\": \"{cropName}\", ";
                    var startPayload = $"\"Timestamp\": {alertTimestamp}, \"Topic\": {mqttPubTopicAlert}, \"DeviceID\": {telemetryData.DeviceID}, \"CropType\": \"{cropName}\", ";

                    alertPayload = "{" + startPayload + alertPayload + "}";

                    // string jsonString = "{\"name\":\"John\", \"age\":30, \"city\":\"New York\"}";
                    // Deserialize JSON string to dynamic object
                    // var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    if (mqttConected && _mqttClient != null)
                    {
                        var messageToMQTT = new MqttApplicationMessageBuilder()
                         .WithTopic(mqttPubTopicAlert)
                         .WithPayload(alertPayload)
                         .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                         .WithRetainFlag(false)
                         .Build();

                        // Publish the message  
                        await _mqttClient.PublishAsync(messageToMQTT);
                        _logger.LogInformation("ALERT PUBLISHED: {alertPayload}", alertPayload);
                    };

                    // ************************ SEND ALERT TO IOT HUB ************  
                    //var alertBytes = Encoding.UTF8.GetBytes(alertPayload);
                    //using Message alertPipeMessage = new(alertBytes);
                    //foreach (KeyValuePair<string, string> prop in message.Properties)
                    //{
                    //    alertPipeMessage.Properties.Add(prop.Key, prop.Value);
                    //}
                    //await _moduleClient!.SendEventAsync("EdgeIoTHub", alertPipeMessage, _cancellationToken);

                }

            }
        }
        catch (Exception ex)
        {
            // Code to handle any other type of exception
            _logger.LogInformation("An error occurred: {ex.Message}", ex.Message);

        }

        return MessageResponse.Completed;
    }

    static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
    {
        try
        {
            Console.WriteLine("Desired property change:");
            Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

            if (desiredProperties["TelemetryInterval"] != null)
                telemetryInterval = desiredProperties["TelemetryInterval"];

        }
        catch (AggregateException ex)
        {
            foreach (Exception exception in ex.InnerExceptions)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", exception);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
        }
        return Task.CompletedTask;
    }


    public DateTime GetTimestamp()
    {
        // Get the Europe/Madrid time zone
        TimeZoneInfo madridTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        // Get current time UTC
        DateTime utcNow = DateTime.UtcNow;
        // Convert current time UTC to Europe/Madrid time
        DateTime madridTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, madridTimeZone);
        return madridTime;
    }

}
