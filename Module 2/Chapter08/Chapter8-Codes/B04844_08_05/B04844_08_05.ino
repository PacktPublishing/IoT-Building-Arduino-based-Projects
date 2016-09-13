#include <SPI.h>
#include <Ethernet.h>
#include <IRremote.h>

byte mac[] = { 0x90, 0xA2, 0xDA, 0x0B, 0x00 and 0xDD };
IPAddress ip(192,168,1,177); 

EthernetServer server(80);

IRsend irsend;
String HTTP_REQUEST;

unsigned int  rawData[69] = {47536, 4700,4250, 750,1500, 700,1500, 700,1550, 700,400, 700,400, 700,400, 700,450, 650,450, 650,1600, 600,1600, 650,1600, 600,500, 600,500, 600,550, 600,500, 600,500, 600,1650, 550,1650, 600,1650, 550,550, 550,600, 500,600, 500,600, 550,550, 550,600, 500,600, 500,600, 500,1750, 500,1700, 500,1750, 500,1700, 500,1750, 500,0};  // SAMSUNG E0E0E01F

void setup()
{
  Serial.begin(9600);

  Ethernet.begin(mac, ip);
  Serial.print("Web Server at: ");
  Serial.println(Ethernet.localIP());

  server.begin();

 
}

void loop()
{
  EthernetClient client = server.available();

  if (client) {
    boolean currentLineIsBlank = true;
    while (client.connected()) {
      if (client.available()) {
        char c = client.read();
        HTTP_REQUEST += c;

        if (c == '\n' && currentLineIsBlank) {

          client.println("HTTP/1.1 200 OK");
          client.println("Content-Type: text/html");
          client.println("Connection: close");
          client.println();
          // send web page
          client.println("<!DOCTYPE html>");
          client.println("<html>");
          client.println("<head>");
          client.println("<title>IR Remote - Internet</title>");

          client.println("</head>");
          client.println("<body>");
          
         

          client.println("<form method=\"get\">");

          if (HTTP_REQUEST.indexOf("GET /?volume=up HTTP/1.1") > -1) {
            //for (int i = 0; i < 3; i++) {
              irsend.sendRaw(rawData,69,32);
              //delay(40);
             //}
            Serial.println("Power button is pressed");
          }

           
            
            client.println("<input type=\"submit\" name=\"key\" value=\"power\">");
            

         
          
          client.println("</form>");

          client.println("</body>");
          client.println("</html>");
          Serial.print(HTTP_REQUEST);
          HTTP_REQUEST = "";
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
  }
}

