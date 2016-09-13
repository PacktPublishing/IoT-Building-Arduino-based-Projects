#include <SPI.h>
#include <WiFi.h>

char ssid[] = "MyHomeWiFi";
char pass[] = "secretPassword";
int keyIndex = 0;

int pin = 2;
volatile unsigned int pulse;
float volume = 0;
float flow_rate = 0;
const int pulses_per_litre = 450;

int status = WL_IDLE_STATUS;

WiFiServer server(80);

void setup()
{

  Serial.begin(9600);
  pinMode(pin, INPUT);
  attachInterrupt(0, count_pulse, RISING);

  if (WiFi.status() == WL_NO_SHIELD)
  {
    Serial.println("WiFi shield not present");
    while (true);
  }

  // attempt to connect to Wifi network:
  while ( status != WL_CONNECTED) {
    Serial.print("Attempting to connect to SSID: ");
    Serial.println(ssid);

    status = WiFi.begin(ssid, pass);

    delay(10000);
    Serial.print("IP Address: ");
    Serial.println(WiFi.localIP());
  }
  server.begin();
}


void loop()
{
  pulse = 0;
  interrupts();
  delay(100);
  noInterrupts();

  Serial.print("Pulses per second: ");
  Serial.println(pulse);

  flow_rate = pulse * 1000 / pulses_per_litre;

  Serial.print("Water flow rate: ");
  Serial.print(flow_rate);
  Serial.println(" milliliters per second");

  volume = volume + flow_rate * 0.1;

  Serial.print("Volume: ");
  Serial.print(volume);
  Serial.println(" milliliters");

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
            while (true);
          } else {
            client.print("Pulses per second: ");
            client.print(pulse);
            client.print("<br>");

            client.print("Water flow rate: ");
            client.print(flow_rate);
            client.print(" milliliters per second<br>");

            client.print("Volume: ");
            client.print(volume);
            client.print(" milliliterstres<br>");
            //end

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

void count_pulse()
{
  pulse++;
}


