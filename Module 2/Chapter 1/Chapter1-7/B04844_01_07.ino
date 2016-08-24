/*Code: B04844_01_07.ino*/

#include <SPI.h>
#include <Ethernet.h>

byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };  

EthernetServer server(80);

void setup()
{
  Serial.begin(9600);
    
  Ethernet.begin(mac);
  server.begin();
  Serial.print("IP Address: ");
  Serial.println(Ethernet.localIP());

}

void loop () {}

