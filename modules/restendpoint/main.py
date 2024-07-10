# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for
# full license information.

import asyncio
import sys
import signal
import threading
from azure.iot.device.aio import IoTHubModuleClient

# Event indicating client stop
stop_event = threading.Event()

def create_client():
    client = IoTHubModuleClient.create_from_edge_environment()

    # Define function for handling received messages
    #async def receive_message_handler(message):
    #    # NOTE: This function only handles messages sent to "input1".
    #    # Messages sent to other inputs, or to the default, will be discarded
    #    if message.input_name == "input1":
    #        print("the data in the message received on input1 was ")
    #        print(message.data)
    #        print("custom properties are")
    #        print(message.custom_properties)
    #        print("forwarding mesage to output1")
    #        await client.send_message_to_output(message, "output1")

    #try:
    #    # Set handler on the client
    #    client.on_message_received = receive_message_handler
    #except:
    #    # Cleanup if failure occurs
    #    client.shutdown()
    #    raise

    return client


async def run_sample(client):
    # Customize this coroutine to do whatever tasks the module initiates
    # e.g. sending messages
    #while True:
    while False:
        await asyncio.sleep(1000)


def main():
    if not sys.version >= "3.5.3":
        raise Exception( "The sample requires python 3.5.3+. Current version of Python: %s" % sys.version )
    print ( "IoT Hub Client for Python" )

    # NOTE: Client is implicitly connected due to the handler being set on it
    client = create_client()

    # Define a handler to cleanup when module is is terminated by Edge
    def module_termination_handler(signal, frame):
        print ("IoTHubClient sample stopped by Edge")
        stop_event.set()

    # Set the Edge termination handler
    signal.signal(signal.SIGTERM, module_termination_handler)

    # Run the sample
    loop = asyncio.get_event_loop()
    try:
        loop.run_until_complete(run_sample(client))
    except Exception as e:
        print("Unexpected error %s " % e)
        raise
    finally:
        print("Shutting down IoT Hub Client...")
        loop.run_until_complete(client.shutdown())
        loop.close()


###################################################
#### REST API & MQTT CONECTION - START
###################################################
import json
import time
from flask import Flask, request, jsonify
import paho.mqtt.client as mqtt_client

app = Flask(__name__)

MQTT_BROKER = '192.168.0.200'
#MQTT_BROKER = 'ubuntu-rpi4'
MQTT_PORT = 1883
MQTT_USER = 'mqtt_user'
MQTT_PASSWORD = 'mqtt_password'
MQTT_CONNECTED = False

# Function to connect to MQTT broker
def connect_mqtt():
    global MQTT_CONNECTED
    client = mqtt_client.Client()
    # client.username_pw_set(MQTT_USER, MQTT_PASSWORD)

    def on_connect(client, userdata, flags, rc):
        global MQTT_CONNECTED
        if rc == 0:
            MQTT_CONNECTED = True
            print("Connected to MQTT Broker!")
        else:
            MQTT_CONNECTED = False
            print("Failed to connect, return code %d\n", rc)

    client.on_connect = on_connect
    client.connect(MQTT_BROKER, MQTT_PORT)
    client.loop_start()

    # Wait for connection
    for _ in range(10):
        if MQTT_CONNECTED:
            break
        time.sleep(1)
    
    if not MQTT_CONNECTED:
        print("Failed to connect to MQTT Broker within the timeout period.")
    
    return client

mqtt_client = connect_mqtt()

# Basic Auth decorator
def check_auth(username, password):
    return username == 'admin' and password == 'secret'

def authenticate():
    return jsonify({"message": "Authentication required."}), 401

def requires_auth(f):
    def decorated(*args, **kwargs):
        auth = request.authorization
        if not auth or not check_auth(auth.username, auth.password):
            return authenticate()
        return f(*args, **kwargs)
    return decorated

@app.route('/api/data', methods=['POST'])
# @requires_auth
def receive_data():
    if not MQTT_CONNECTED:
        return jsonify({"message": "Failed to connect to MQTT Broker."}), 500
    
    data = request.get_data()
    content_type = request.content_type

    if content_type == 'application/json':
        #print("content_type = application/json")
        data = request.json
        data_str = json.dumps(data)
    elif content_type == 'application/octet-stream':
        #print("content_type = application/octet-stream")
        data_str = data.hex()
    else:
        #print("decode utf-8")
        data_str = data.decode('utf-8')
        
    result = mqtt_client.publish('restendpoint/telemetry', data_str)
    
    # print("MQTT publish result", result)
    
    #if result.rc == mqtt_client.MQTT_ERR_SUCCESS:
    if result.rc == 0:
        #print("Data received and sent to MQTT broker.")
        return jsonify({"message": "Data received and sent to MQTT broker."})
    else:
        #print("Failed to send data to MQTT broker.")
        return jsonify({"message": "Failed to send data to MQTT broker."}), 500

###################################################
#### REST API & MQTT CONECTION - END
###################################################

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5678)
    main()