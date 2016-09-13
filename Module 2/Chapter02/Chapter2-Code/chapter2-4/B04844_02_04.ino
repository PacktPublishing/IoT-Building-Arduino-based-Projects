#include <SPI.h>
#include <WiFi.h>

char ssid[] = "Dialog 4G";
char pass[] = "secretPassword";

void setup()
{
 WiFi.begin(ssid, pass);

  if (WiFi.status() != WL_CONNECTED) 
  { 
    Serial.println("Couldn't get a wifi connection");
    while(true);
  } 
 
  else 
  {
  long rssi = WiFi.RSSI();
  Serial.print("RSSI:");
  Serial.println(rssi);
  }
}

void loop () 
{
}

