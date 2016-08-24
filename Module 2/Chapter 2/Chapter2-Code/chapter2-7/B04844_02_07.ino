#include <SPI.h>
#include <WiFi.h>


char ssid[] = "yourNetwork";
char pass[] = "secretPassword";
int keyIndex = 0;

int status = WL_IDLE_STATUS;

WiFiServer server(80);

void setup() 
{

  Serial.begin(9600); 
  while (!Serial) 
  {
    ; 
  }
  
 
  if (WiFi.status() == WL_NO_SHIELD) 
  {
    Serial.println("WiFi shield not present"); 
    while(true);
  } 
  
  // attempt to connect to Wifi network:
  while ( status != WL_CONNECTED) { 
    Serial.print("Attempting to connect to SSID: ");
    Serial.println(ssid);
    
    status = WiFi.begin(ssid, pass);

    delay(10000);
  } 
  server.begin();
}


void loop() 
{

  WiFiClient client = server.available();
  if (client) {
    Serial.println("new client");

    boolean currentLineIsBlank = true;
    while (client.connected()) {
      if (client.available()) {
        char c = client.read();
        Serial.write(c);
      
        if (c == '\n' && currentLineIsBlank) {
         
          client.println("HTTP/1.1 200 OK");
          client.println("Content-Type: text/html");
          client.println("Connection: close"); 
          client.println("Refresh: 5"); 
          client.println();
          client.println("<!DOCTYPE HTML>");
          client.println("<html>");

  if (WiFi.status() != WL_CONNECTED) { 
    client.println("Couldn't get a wifi connection");
    while(true);
  } 

  else {
                      
  long rssi = WiFi.RSSI();
  client.print("signal strength (RSSI):");
  client.print(rssi);
  client.println(" dBm"); 
}         

client.println("</html>");
           break;
        }
        if (c == '\n') {
        
          currentLineIsBlank = true;
        } 
        else if (c != '\r') {
          
          currentLineIsBlank = false;
        }
      }
    }
   
    delay(1);
    

    client.stop();
    Serial.println("client disonnected");
  }
}

