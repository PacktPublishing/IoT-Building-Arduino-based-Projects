#include <SPI.h>
#include <Ethernet.h>

byte mac[] = { 0x90, 0xA2, 0xDA, 0x0B, 0x00 and 0xDD };
IPAddress ip(192,168,1,177); 

EthernetServer server(80);

String httpRequest;
boolean relayStatus = 0;

void setup()
{
  Serial.begin(9600);

  Ethernet.begin(mac, ip);
  Serial.print("Web Server at: ");
  Serial.println(Ethernet.localIP());

  server.begin();

  pinMode(9, OUTPUT);
}

void loop()
{
  EthernetClient client = server.available();

  if (client) {
    boolean currentLineIsBlank = true;
    while (client.connected()) {
      if (client.available()) {
        char c = client.read();
        httpRequest += c;

        if (c == '\n' && currentLineIsBlank) {

          client.println("HTTP/1.1 200 OK");
          client.println("Content-Type: text/html");
          client.println("Connection: close");
          client.println();
          // send web page
          client.println("<!DOCTYPE html>");
          client.println("<html>");
          client.println("<head>");
          client.println("<title>Internet Controlled Power Switch</title>");
          client.println("</head>");
          client.println("<body>");
          client.println("<h1>Internet Controlled Power Switch</h1>");
          client.println("<p>Click radio buttons to toggle the switch.</p>");

          client.println("<form method=\"get\">");

          if (httpRequest.indexOf("GET /?switch=0 HTTP/1.1") > -1) {
            relayStatus = 0;
            digitalWrite(9, LOW);
            Serial.println("Off Clicked");
          } else if (httpRequest.indexOf("GET /?switch=1 HTTP/1.1") > -1) {
            relayStatus = 1;
            digitalWrite(9, HIGH);
            Serial.println("On Clicked");
          }

          if (relayStatus) {
            client.println("<input type=\"radio\" name=\"switch\" value=\"1\" checked>ON");
            client.println("<input type=\"radio\" name=\"switch\" value=\"0\" onclick=\"submit();\" >OFF");
          }
          else {
            client.println("<input type=\"radio\" name=\"switch\" value=\"1\" onclick=\"submit();\" >ON");
            client.println("<input type=\"radio\" name=\"switch\" value=\"0\" checked>OFF");
          }
          client.println("</form>");

          client.println("</body>");
          client.println("</html>");
          Serial.print(httpRequest);
          httpRequest = "";
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

