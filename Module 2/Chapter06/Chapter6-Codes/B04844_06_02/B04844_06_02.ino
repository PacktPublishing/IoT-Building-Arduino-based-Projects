#include <TinyGPS++.h>
#include <Ethernet.h>
#include <SPI.h>
#include <SoftwareSerial.h>
/*
 This example uses software serial and the TinyGPS++ library by Mikal Hart
 Based on TinyGPSPlus/DeviceExample.ino by Mikal Hart
 Modified by acavis
*/

// Choose two Arduino pins to use for software serial
// The GPS Shield uses D2 and D3 by default when in DLINE mode
int RXPin = 2;
int TXPin = 3;

// The Skytaq EM-506 GPS module included in the GPS Shield Kit
// uses 4800 baud by default
int GPSBaud = 4800;

// Create a TinyGPS++ object called "gps"
TinyGPSPlus gps;

// Create a software serial port called "gpsSerial"
SoftwareSerial gpsSerial(RXPin, TXPin);

byte mac[] = {
  0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED
};
IPAddress ip(192, 168, 1, 177);

// Initialize the Ethernet server library
// with the IP address and port you want to use
// (port 80 is default for HTTP):
EthernetServer server(80);
EthernetClient client;

void setup()
{
  // Start the Arduino hardware serial port at 9600 baud
  Serial.begin(9600);

  // Start the software serial port at the GPS's default baud
  gpsSerial.begin(GPSBaud);
  
   // start the Ethernet connection and the server:
  Ethernet.begin(mac, ip);
  server.begin();
  
}

void loop()
{
  client = server.available();
  // This sketch displays information every time a new sentence is correctly encoded.
  while (gpsSerial.available() > 0)
    if (gps.encode(gpsSerial.read()))
      displayInfo();

  // If 5000 milliseconds pass and there are no characters coming in
  // over the software serial port, show a "No GPS detected" error
  if (millis() > 5000 && gps.charsProcessed() < 10)
  {
    Serial.println(F("No GPS detected"));
    while(true);
  }
}

void displayInfo()
{
  
  
  if (client) {
    Serial.println("new client");
    // an http request ends with a blank line
    boolean currentLineIsBlank = true;
    while (client.connected()) {
      if (client.available()) {
        char c = client.read();
        Serial.write(c);
        // if you've gotten to the end of the line (received a newline
        // character) and the line is blank, the http request has ended,
        // so you can send a reply
        if (c == '\n' && currentLineIsBlank) {
          // send a standard http response header
          client.println("HTTP/1.1 200 OK");
          client.println("Content-Type: text/html");
          client.println("Connection: close");  // the connection will be closed after completion of the response
          client.println("Refresh: 5");  // refresh the page automatically every 5 sec
          client.println();
          client.println("<!DOCTYPE HTML>");
          client.println("<html>");
          
          client.println("<head>");
          client.println("<meta name=\"viewport\" content=\"initial-scale=1.0, user-scalable=no\">");
          client.println("<meta charset=\"utf-8\">");
          client.println("<title>Simple markers</title>");
          client.println("<style>");
          client.println("html, body, #map-canvas {height: 100%;margin: 0;padding: 0;}");

          client.println("</style>");
          client.println("<script src=\"https://maps.googleapis.com/maps/api/js?v=3.exp&signed_in=true\"></script>");
          client.println("<script>");
          client.println("function initialize() {");
          client.println("var myLatlng = new google.maps.LatLng(gps.location.lat(),gps.location.lng());");
          client.println("var mapOptions = {");
          client.println("zoom: 4,");
          client.println("center: myLatlng");
          client.println("}");
          client.println("var map = new google.maps.Map(document.getElementById('map-canvas'), mapOptions);");

          client.println("var marker = new google.maps.Marker({");
          client.println("position: myLatlng,");
          client.println("map: map,");
          client.println("title: 'Hello World!'");
          client.println("});");
          client.println("}");

          client.println("google.maps.event.addDomListener(window, 'load', initialize);");

          client.println("</script>");
          client.println("</head>");
          client.println("<body>");
          client.println("<div id=\"map-canvas\"></div>");
          client.println("</body>");
  
          client.println("</html>");
          break;
        }
        if (c == '\n') {
          // you're starting a new line
          currentLineIsBlank = true;
        }
        else if (c != '\r') {
          // you've gotten a character on the current line
          currentLineIsBlank = false;
        }
      }
    }
    // give the web browser time to receive the data
    delay(1);
    // close the connection:
    client.stop();
    Serial.println("client disconnected");
  }
  
}

