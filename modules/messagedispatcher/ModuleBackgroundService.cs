using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Text;

namespace messagedispatcher;

internal class ModuleBackgroundService : BackgroundService
{
    private int _counter;
    private ModuleClient? _moduleClient;
    private CancellationToken _cancellationToken;
    private readonly ILogger<ModuleBackgroundService> _logger;

    public ModuleBackgroundService(ILogger<ModuleBackgroundService> logger) => _logger = logger;

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

        _logger.LogInformation("MESSAGE DISPATCHER module client initialized.");

        // Register callback to be called when a message is received by the module
        await _moduleClient.SetInputMessageHandlerAsync("inputFromMQTT", ProcessMessageAsync, null, cancellationToken);
    }

    async Task<MessageResponse> ProcessMessageAsync(Message message, object userContext)
    {

        try
        {
            int counterValue = Interlocked.Increment(ref _counter);

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            _logger.LogInformation("Received message: {counterValue}, Body: [{messageString}]", counterValue, messageString);

            // Create an instance of DataParser with the input string
            DataParser parser = new DataParser(messageString);
            StringFinder stringFinder = new StringFinder();

            //  IdNode Extractor
            var extractor = new IdNodeExtractor();
            var myIdNode = extractor.ExtractIdNode(parser.MyPayload);
            // _logger.LogInformation("ID Node: {myIdNode}", myIdNode);

            // Check IdNode in "iot-devices.json" list
            string filePath = "/app/data/iot-devices.json"; // Ruta del archivo JSON
            IoTDeviceManager manager = new IoTDeviceManager(filePath);
            IoTDevice device = manager.GetDeviceById($"{myIdNode}");

            bool deviceIsFound = false;
            string whatDataSource = "";
            bool sendToMonitor = false;
            string cropType = "";

            // Show found device values
            if (device != null)
            {
                _logger.LogInformation("Found IdNode: {device.IdNode}  Device: {device.Device} Datasource: {device.Datasource}", device.IdNode, device.Device, device.Datasource);
                deviceIsFound = true;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                whatDataSource = device.Datasource;
                cropType = device.Crop;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                sendToMonitor = device.SendToMonitor;
            }
            else
            {
                _logger.LogInformation("Device NOT FOUND:  {myIdNode}  ", myIdNode);
            }

            // The device exists
            if (deviceIsFound)
            {
                string messageToSend = "";
                // Determine the data source
                NormalizedData normalizedData;
#pragma warning disable CS8604 // Possible null reference argument.
                switch (whatDataSource)
                {
                    case "ChirpStack":  // ChirpStack LoRa Gateway
                        ChirpStackData chirpData = new ChirpStackData(parser.MyTimestamp, cropType, parser.MyTopic, parser.MyPayload);
                        normalizedData = chirpData.ToNormalizedData();
                        messageToSend = normalizedData.ToJson();
                        break;
                    case "libelium": // Libelium Gateway
                        LibeliumData libeliumData = new LibeliumData(parser.MyTimestamp, cropType, parser.MyTopic, parser.MyPayload);
                        normalizedData = libeliumData.ToNormalizedData();
                        messageToSend = normalizedData.ToJson();
                        break;
                    case "Node-RED": // NodeRED Simulator  
                        NodeREDData nodeRedData = new NodeREDData(parser.MyTimestamp, cropType, parser.MyTopic, parser.MyPayload);
                        normalizedData = nodeRedData.ToNormalizedData();
                        messageToSend = normalizedData.ToJson();
                        break;
                    case "csv": // Simple payload with values ​​separated by comma - TEST
                        CsvData csvData = new CsvData(parser.MyTimestamp, cropType, parser.MyTopic, parser.MyPayload);
                        normalizedData = csvData.ToNormalizedData();
                        messageToSend = normalizedData.ToJson();
                        break;
                    default:
                        _logger.LogInformation("No datasource found to device:  {myIdNode}  ", myIdNode);
                        break;
                }
#pragma warning restore CS8604 // Possible null reference argument.

                // if the values has to be sent to monitor
                if (sendToMonitor)
                {
                    messageBytes = Encoding.UTF8.GetBytes(messageToSend);

                    if (!string.IsNullOrEmpty(messageToSend))
                    {
                        using Message pipeMessage = new(messageBytes);
                        foreach (KeyValuePair<string, string> prop in message.Properties)
                        {
                            pipeMessage.Properties.Add(prop.Key, prop.Value);
                        }
                        await _moduleClient!.SendEventAsync("messagedispatcherOutput", pipeMessage, _cancellationToken);

                        // _logger.LogInformation("Message SENT");
                        _logger.LogInformation("Message SENT: {messageToSend}", messageToSend);
                    }
                }

            } // device is found

        }
        catch (Exception ex)
        {
            // Code to handle any other type of exception
            _logger.LogInformation("An error occurred: {ex.Message}", ex.Message);

        }

        return MessageResponse.Completed;
    }
}
