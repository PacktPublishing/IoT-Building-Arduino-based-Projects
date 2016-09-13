#include <SPI.h>
#include <WiFi.h>

char ssid[] = "Dialog 4G Open";
int status = WL_IDLE_STATUS;

void setup() 
{
  Serial.begin(9600); 
  while (!Serial) 
  {
    ;
  }
  
  if (WiFi.status() == WL_NO_SHIELD) 
  {
    Serial.println("No WiFi shield found"); 
    while(true);
  } 
  
  while ( status != WL_CONNECTED) 
  { 
    Serial.print("Attempting to connect to open SSID: ");
    Serial.println(ssid);
    status = WiFi.begin(ssid);

    delay(10000);
  }
   
  Serial.print("You're connected to the network");
}

void loop () 
{
  
}

