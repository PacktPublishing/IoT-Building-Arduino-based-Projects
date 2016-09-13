#include <SPI.h>
#include <Ethernet.h>

byte mac[] = { 0x90, 0xA2, 0xDA, 0x0B, 0x00 and 0xDD };
IPAddress ip(192,168,1,177); 

EthernetServer server(80);

String http_Request;
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
        http_Request += c;

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

          client.println("<script src=\"https://metroui.org.ua/js/jquery-2.1.3.min.js\"></script>");
          client.println("<script src=\"https://metroui.org.ua/js/metro.js\"></script>");
          client.println("<link rel=\"stylesheet\" href=\"https://metroui.org.ua/css/metro.css\">");

          client.println("</head>");
          client.println("<body>");
          client.println("<h1>Internet Controlled Power Switch</h1>");
          client.println("<p>Click radio buttons to toggle the switch.</p>");

          client.println("<form method=\"get\">");

          if (http_Request.indexOf("GET /?switch=0 HTTP/1.1") > -1) {
            relayStatus = 0;
            digitalWrite(9, LOW);
            Serial.println("Off Clicked");
          } else if (http_Request.indexOf("GET /?switch=1 HTTP/1.1") > -1) {
            relayStatus = 1;
            digitalWrite(9, HIGH);
            Serial.println("On Clicked");
          }

           if (relayStatus) {
            client.println("<label class=\"input-control radio\">");
            client.println("<input type=\"radio\" name=\"switch\" value=\"1\" checked><span class=\"check\"></span>ON");
            client.println("</label>");

            client.println("<label class=\"input-control radio\">");
            client.println("<input type=\"radio\" name=\"switch\" value=\"0\" onclick=\"submit();\"><span class=\"check\"></span>OFF");
            client.println("</label>");
          }
          else {
            client.println("<label class=\"input-control radio\">");
            client.println("<input type=\"radio\" name=\"switch\" value=\"1\" onclick=\"submit();\"><span class=\"check\"></span>ON");
            client.println("</label>");

            client.println("<label class=\"input-control radio\">");
            client.println("<input type=\"radio\" name=\"switch\" value=\"0\" checked><span class=\"check\"></span>OFF");
            client.println("</label>");
          }
          
          client.println("</form>");

          client.println("</body>");
          client.println("</html>");
          Serial.print(http_Request);
          http_Request = "";
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

