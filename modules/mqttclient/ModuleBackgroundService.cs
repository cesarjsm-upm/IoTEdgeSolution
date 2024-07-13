using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Text;

//
using MQTTnet;
using MQTTnet.Client;
// using Newtonsoft.Json;

namespace mqttclient;

internal class ModuleBackgroundService : BackgroundService
{
    private int _counter;
    private ModuleClient? _moduleClient;
    private CancellationToken _cancellationToken;
    private readonly ILogger<ModuleBackgroundService> _logger;

    public ModuleBackgroundService(ILogger<ModuleBackgroundService> logger) => _logger = logger;


    // ************* MQTT CLIENT *************************
    // private IMqttClient _mqttClient;
    private static IMqttClient? _mqttClient = null;
    private string mqttclientAddress = "192.168.0.200";
    private int mqttclientPort = 1883;
    //private string mqttclientAddress = "ubuntu-rpi4";
    // private string mqttSubTopic = "#";
    private string mqttSubTopic = "+/telemetry";


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

        _logger.LogInformation("MQTT CLIENT module initialized.");

        // Setup MQTT client options
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithClientId("mqttclient")
            .WithTcpServer(mqttclientAddress, mqttclientPort) 
            .WithCleanSession()
            .Build();

        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateMqttClient();
        var connectResult = await _mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);

        if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
        {
            _logger.LogInformation("Connected to MQTT BROKER: {mqttclientAddress} Topic {mqttSubTopic}",
                                    mqttclientAddress, mqttSubTopic);
            // Subscribe to a topic
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(mqttSubTopic).Build());
        };

        _mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
        
    }

    private Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
        _logger.LogInformation("Received message: {topic} : {payload}", topic, payload);

        string contentType = "text";
        if (JsonValidator.IsValidJson(payload)) { contentType = "json"; }

        // Get the Europe/Madrid time zone
        TimeZoneInfo madridTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        DateTime utcNow = DateTime.UtcNow;  // Get current time UTC
        DateTime madridTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, madridTimeZone); // Convert current time UTC to Europe/Madrid time

        var mqttMessage = new MqttMessage(topic, payload, contentType, madridTime);
        var strMqttMessage = mqttMessage.ToString();
        var messageBytes = Encoding.UTF8.GetBytes(strMqttMessage);

        using (var pipeMessage = new Message(messageBytes))
        {
            _moduleClient!.SendEventAsync("mqttclientOutput", pipeMessage, _cancellationToken).Wait();
            _logger.LogInformation("Message SENT: {strMqttMessage}", strMqttMessage);
        }

        return Task.CompletedTask;
    }


    async Task<MessageResponse> ProcessMessageAsync(Message message, object userContext)
    {
        int counterValue = Interlocked.Increment(ref _counter);

        byte[] messageBytes = message.GetBytes();
        string messageString = Encoding.UTF8.GetString(messageBytes);
        _logger.LogInformation("Received message: {counterValue}, Body: [{messageString}]", counterValue, messageString);

        if (!string.IsNullOrEmpty(messageString))
        {
            using var pipeMessage = new Message(messageBytes);
            foreach (var prop in message.Properties)
            {
                pipeMessage.Properties.Add(prop.Key, prop.Value);
            }
            await _moduleClient!.SendEventAsync("mqttclientOutput", pipeMessage, _cancellationToken);

            _logger.LogInformation("Received message sent - mqttclientOutput");
        }
        return MessageResponse.Completed;
    }
    
}
