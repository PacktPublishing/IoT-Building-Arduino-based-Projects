#include <TinyGPS++.h>
#include <Ethernet.h>
#include <SPI.h>
#include <SoftwareSerial.h>
#include <Dhcp.h>
#include <Dns.h>
#include <Ethernet.h>
#include <EthernetClient.h>
#include <Temboo.h>

#include "TembooAccount.h" // Contains Temboo account information

int RXPin = 2;
int TXPin = 3;

int GPSBaud = 4800;

TinyGPSPlus gps;

SoftwareSerial gpsSerial(RXPin, TXPin);

byte mac[] = {
  0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED
};
IPAddress ip(192, 168, 1, 177);

EthernetServer server(80);
EthernetClient client;

void setup()
{
 
  Serial.begin(9600);

  gpsSerial.begin(GPSBaud);
  
  Ethernet.begin(mac, ip);
  server.begin();
  
}

void loop()
{
  TembooChoreo SendSMSChoreo(client);

    // Invoke the Temboo client
    SendSMSChoreo.begin();

    // Set Temboo account credentials
    SendSMSChoreo.setAccountName(TEMBOO_ACCOUNT);
    SendSMSChoreo.setAppKeyName(TEMBOO_APP_KEY_NAME);
    SendSMSChoreo.setAppKey(TEMBOO_APP_KEY);

    // Set Choreo inputs
    String BodyValue =  "Latitude:" + String(gps.location.lat())+ " longitude:"+ String(gps.location.lng());
    SendSMSChoreo.addInput("Body", BodyValue);
    String ToValue = "+16175XXX213";
    SendSMSChoreo.addInput("To", ToValue);
    String FromValue = "+16175XXX212";
    SendSMSChoreo.addInput("From", FromValue);

    // Identify the Choreo to run
    SendSMSChoreo.setChoreo("/Library/Twilio/SMSMessages/SendSMS");

    // Run the Choreo; when results are available, print them to serial
    SendSMSChoreo.run();

    while(SendSMSChoreo.available()) {
      char c = SendSMSChoreo.read();
      Serial.print(c);
    }
    SendSMSChoreo.close();
  

  Serial.println("\nWaiting...\n");
  delay(1800*1000); // wait 30 minutes between SendSMS calls
}



