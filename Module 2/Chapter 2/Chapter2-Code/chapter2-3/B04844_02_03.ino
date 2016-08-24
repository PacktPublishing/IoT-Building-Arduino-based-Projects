#include <SPI.h>
#include <WiFi.h>

char ssid[] = "Dialog 4G";
char key[] = "D0D0DEADF00DABBADEAFBEADED";
int keyIndex = 0;
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
    Serial.println("WiFi shield not present"); 
 
    while(true);
  } 

  while ( status != WL_CONNECTED) 
  { 
    Serial.print("Attempting to connect to WEP network, SSID: ");
    Serial.println(ssid);
    status = WiFi.begin(ssid, keyIndex, key);

    delay(10000);
  }

  Serial.print("You're connected to the network");
 }

void loop() 
{

}

