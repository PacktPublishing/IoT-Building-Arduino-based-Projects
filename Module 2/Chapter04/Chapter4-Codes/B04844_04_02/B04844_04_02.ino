/* Setup shield-specific #include statements */
#include <SPI.h>
#include <Dhcp.h>
#include <Dns.h>
#include <Ethernet.h>
#include <EthernetClient.h>
#include <Temboo.h>
#include "TembooAccount.h" // Contains Temboo account information

byte ethernetMACAddress[] = ETHERNET_SHIELD_MAC;
EthernetClient client;

int numRuns = 1;   // Execution count, so this doesn't run forever
int maxRuns = 10;   // Maximum number of times the Choreo should be executed

void setup() {
  Serial.begin(9600);
  
  // For debugging, wait until the serial console is connected
  delay(4000);
  while(!Serial);

  Serial.print("DHCP:");
  if (Ethernet.begin(ethernetMACAddress) == 0) {
    Serial.println("FAIL");
    while(true);
  }
  Serial.println("OK");
  delay(5000);

  Serial.println("Setup complete.\n");
}

void loop() {
  if (numRuns <= maxRuns) {
    Serial.println("Running Upload - Run #" + String(numRuns++));

    TembooChoreo UploadChoreo(client);

    // Invoke the Temboo client
    UploadChoreo.begin();

    // Set Temboo account credentials
    UploadChoreo.setAccountName(TEMBOO_ACCOUNT);
    UploadChoreo.setAppKeyName(TEMBOO_APP_KEY_NAME);
    UploadChoreo.setAppKey(TEMBOO_APP_KEY);

    // Set Choreo inputs
    String APIKeyValue = "0c62beaaxxxxxxxxxxxxxx3845ca";
    UploadChoreo.addInput("APIKey", APIKeyValue);
    String AccessTokenValue = "721576556518xxxxx-xxxxxxa84d7b8e01";
    UploadChoreo.addInput("AccessToken", AccessTokenValue);
    String AccessTokenSecretValue = "d95e8xxxxxx4fddb7";
    UploadChoreo.addInput("AccessTokenSecret", AccessTokenSecretValue);
    String APISecretValue = "7277dxxxxxx7d696";
    UploadChoreo.addInput("APISecret", APISecretValue);
    String URLValue = "https://www.arduino.cc/en/uploads/Main/ArduinoEthernetFront450px.jpg";
    UploadChoreo.addInput("URL", URLValue);

    // Identify the Choreo to run
    UploadChoreo.setChoreo("/Library/Flickr/Photos/Upload");

    // Run the Choreo; when results are available, print them to serial
    UploadChoreo.run();

    while(UploadChoreo.available()) {
      char c = UploadChoreo.read();
      Serial.print(c);
    }
    UploadChoreo.close();
  }

  Serial.println("\nWaiting...\n");
  delay(30000); // wait 30 seconds between Upload calls
}

