// Libaries
#include <Arduino.h>
#include <DHT.h>
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
#include <NTPClient.h>
#include <WiFiUdp.h>

// verbose logging
// #define DEBUG_ESP_HTTP_CLIENT
// #define DEBUG_ESP_PORT Serial
// #include <ESP8266WiFi.h>

// Board type:
// NodeMCU (1.0 ESP-12E Module esp8266)

//////////////////////////////
// IMPORTANT: Update these values for each sensor
const int sensor_id = 14;
const char* farm_password = "foobar";

// Wifi Info
// Define an array to store multiple WiFi credentials
const char* ssids[] = {"EssexFarmNew", "Eskilstark", "Echo_Farm_5G", "Echo _Farm", "Echo Farm", "Farmhouse_5G", "Farmhouse 5G"};
const char* passwords[] = {"", "essexcounty?", "litzington", "litzington", "litzington", "litzington", "litzington"};
const int numNetworks = sizeof(ssids) / sizeof(ssids[0]);

// Sensor API info
const char* serverName = "http://temperatures.chickenkiller.com/api/v1/reading";
const char* heartbeatName = "http://temperatures.chickenkiller.com/api/v1/heartbeat";
//////////////////////////////


// Sensor variables
uint8_t DHTPin = D3;
#define DHTTYPE DHT22

// Loop every 5 seconds on error (5000)
unsigned long scriptErrorLoopDelay = 5000;

// Loop every minute on success (60000)
unsigned long scriptSuccessLoopDelay = 60000;
// unsigned long scriptSuccessLoopDelay = 2000;

// Define NTP Client to get time
// const long utcOffsetInSeconds = -18000;  // New York
const long utcOffsetInSeconds = 0;  // UTC is fine
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", utcOffsetInSeconds);

// Define sensor settings
DHT dht_sensor(DHTPin, DHTTYPE);

// Let magic
#define LED 2

// Initialize WiFi
void initWifi() {
  Serial.println("Log: Attempting to connect to WiFi");

  int networkIndex = 0; // Start with first network

  while (true) {
    Serial.print("Log: Trying to connect to: ");
    Serial.println(ssids[networkIndex]);

    WiFi.begin(ssids[networkIndex], passwords[networkIndex]);

    // Try to connect with a timeout
    unsigned long startAttemptTime = millis();
    bool isConnected = false;

    // Change this value to increase or decrease the connection attempt timeout per network
    const unsigned long timeoutMillis = 10000; // 5 second timeout per network

    while (millis() - startAttemptTime < timeoutMillis) {
      if (WiFi.status() == WL_CONNECTED) {
        isConnected = true;
        Serial.println("\nLog: Connected to WiFi network with IP Address: ");
        Serial.println(WiFi.localIP());
        return; // Exit initWiFi function as we are now connected
      }
      delay(500);
      Serial.print(".");
    }

    if (!isConnected) {
      Serial.println("\nLog: Failed to connect to WiFi network.");
      WiFi.disconnect();
    }

    // Try next network or start over if at the end of list
    networkIndex = (networkIndex + 1) % numNetworks;
  }
}

// Run once
void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);

  // Enable debugging output
  // Serial.setDebugOutput(true);

  // Initialize wifi
  initWifi();

  // Initialize time client
  timeClient.begin();
  timeClient.setTimeOffset(utcOffsetInSeconds);

  // Pin
  pinMode(LED, OUTPUT);

  // Sensor
  dht_sensor.begin();  // initialize the DHT sensor
  delay(2000); // Wait for 2 seconds to allow DHT sensor to stabilize
}


void loop() {
  Serial.println("------- Loop -------");
  float humi = dht_sensor.readHumidity();
  float tempC = dht_sensor.readTemperature();
  float tempF = dht_sensor.readTemperature(true);

  if (isnan(tempF) || isnan(humi)) {
    Serial.println("Log: Failed to read from DHT sensor!");

    if (WiFi.status() == WL_CONNECTED) {
        sensorNotConnected();
    } else {
        wifiAndSensorNotConnected();
    }
  } else {
    digitalWrite(LED, HIGH);
    Serial.println("Log: Successfully read from DHT sensor");

    if (WiFi.status() == WL_CONNECTED) {
        wifiAndSensorsConnected(tempF, humi);
    } else {
        wifiNotConnected();
    }
  }  
}


void sensorNotConnected() {
    Serial.println("Log: Connected to WiFi");

    // HTTP/WIFI seutp
    WiFiClient client;

    // for https:
    // WiFiClientSecure client; // for
    // client.setInsecure();  // Could use setFingerPrint when I understand how that works

    HTTPClient http;
    http.begin(client, heartbeatName);

    // Request Headers
    http.addHeader("Content-Type", "application/json");

    // Request Data (JSON)
    String httpRequestData = "{";
    httpRequestData = httpRequestData + "\"SensorID\": \"" + sensor_id + "\", ";
    httpRequestData = httpRequestData + "\"Password\": \"" + farm_password + "\"";
    httpRequestData = httpRequestData + "}";

    Serial.print("Log: Post Data: ");
    Serial.println(httpRequestData);

    int httpResponseCode = http.POST(httpRequestData);

    Serial.print("Log: HTTP Response code: ");
    Serial.println(httpResponseCode);

    if (httpResponseCode > 0) {
        String payload = http.getString();
        Serial.print("Log: HTTP Payload: ");
        Serial.println(payload);
    } else {
        Serial.println();
        Serial.printf("[HTTP] ... failed, error: %s\n", http.errorToString(httpResponseCode).c_str());
    }

    // Free resources
    http.end();

    // End of loop, wait one minute
    digitalWrite(LED, LOW);
    Serial.print("------- End of Loop ------- ");
    Serial.println();
    delay(scriptSuccessLoopDelay);
}


void wifiAndSensorNotConnected() {
    // Loop error
    digitalWrite(LED, LOW);
    Serial.print("------- End of Loop ------- ");
    Serial.println();
    dht_sensor.begin();  // initialize the DHT sensor
    delay(scriptErrorLoopDelay);
}


void wifiNotConnected() {
    Serial.println("Log: ERROR Not connected to WiFi");
    // Loop error
    digitalWrite(LED, LOW);
    Serial.print("------- End of Loop ------- ");
    Serial.println();
    initWifi();
    delay(scriptErrorLoopDelay);
}


void wifiAndSensorsConnected(float tempF, float humi) {
    Serial.println("Log: Connected to WiFi");

    // Get and Format date according to http://www.stensat.org/docs/sys395/11_ntp_time.pdf
    struct tm ts;
    char buf[80];
    timeClient.update();
    String tim = timeClient.getFormattedTime();
    time_t ttag = timeClient.getEpochTime();
    tim = tim + " " + String(ttag);
    ts = *localtime(&ttag);
    strftime(buf, 80, "%Y-%m-%dT%H:%M:%SZ", &ts);

    // HTTP/WIFI seutp
    WiFiClient client;

    // for https:
    // WiFiClientSecure client; 
    // client.setInsecure();  // Could use setFingerPrint when I understand how that works

    HTTPClient http;
    http.begin(client, serverName);

    // Request Headers
    http.addHeader("Content-Type", "application/json");

    // Request Data (JSON)
    String httpRequestData = "{";
    httpRequestData = httpRequestData + "\"SensorID\": \"" + sensor_id + "\", ";
    httpRequestData = httpRequestData + "\"Password\": \"" + farm_password + "\", ";
    httpRequestData = httpRequestData + "\"TempF\": \"" + tempF + "\", ";
    httpRequestData = httpRequestData + "\"Humidity\": \"" + humi + "\", ";
    httpRequestData = httpRequestData + "\"TimeStamp\": \"" + buf + "\"";
    httpRequestData = httpRequestData + "}";

    Serial.print("Log: Post Data: ");
    Serial.println(httpRequestData);

    int httpResponseCode = http.POST(httpRequestData);

    Serial.print("Log: HTTP Response code: ");
    Serial.println(httpResponseCode);

    if (httpResponseCode > 0) {
        String payload = http.getString();
        Serial.print("Log: HTTP Payload: ");
        Serial.println(payload);
    } else {
        Serial.println();
        Serial.printf("[HTTP] ... failed, error: %s\n", http.errorToString(httpResponseCode).c_str());
    }

    // Free resources
    http.end();

    // End of loop, wait one minute
    digitalWrite(LED, LOW);
    Serial.print("------- End of Loop ------- ");
    Serial.println();
    delay(scriptSuccessLoopDelay);
}