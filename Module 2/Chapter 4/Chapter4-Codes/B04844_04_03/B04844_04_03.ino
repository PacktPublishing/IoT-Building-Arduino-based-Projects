/* Setup shield-specific #include statements */
#include <SPI.h>
#include <Dhcp.h>
#include <Dns.h>
#include <Ethernet.h>
#include <EthernetClient.h>
#include <Temboo.h>
#include "TembooAccount.h" // Contains Temboo account information
#include <Adafruit_VC0706.h>
#include <SoftwareSerial.h>
#include <Base64.h>


byte ethernetMACAddress[] = ETHERNET_SHIELD_MAC;
EthernetClient client;

char encodedStringBuffer;

SoftwareSerial cameraconnection = SoftwareSerial(2, 3); // Arduino RX, TX
Adafruit_VC0706 cam = Adafruit_VC0706(&cameraconnection);


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

  if (cam.begin()) {
    Serial.println("Camera Found:");
  } else {
    Serial.println("No camera found");
    return;
  }
 
  // Set the picture size
  cam.setImageSize(VC0706_640x480);      
}

 


void loop() {
  if (! cam.takePicture()) {
    Serial.println("Failed to snap.\n");
}

  else
{
    Serial.println("Picture taken.\n");
  
  // Get the size of the image (frame) taken
  uint16_t jpglen = cam.frameLength();



    TembooChoreo UploadChoreo(client);

    // Invoke the Temboo client
    UploadChoreo.begin();

    // Set Temboo account credentials
    UploadChoreo.setAccountName(TEMBOO_ACCOUNT);
    UploadChoreo.setAppKeyName(TEMBOO_APP_KEY_NAME);
    UploadChoreo.setAppKey(TEMBOO_APP_KEY);

    // Set Choreo inputs
    String APIKeyValue = "0c62beaa1xxxxxxxxxa94fce3845ca";
    UploadChoreo.addInput("APIKey", APIKeyValue);
    String AccessTokenValue = "7215765565xxxxxxx-xxxxxxxx4d7b8e01";
    UploadChoreo.addInput("AccessToken", AccessTokenValue);
    String AccessTokenSecretValue = "d95e8xxxxxxxfddb7";
    UploadChoreo.addInput("AccessTokenSecret", AccessTokenSecretValue);
    String APISecretValue = "7277dxxxxxxxx7d696";
    UploadChoreo.addInput("APISecret", APISecretValue);

while (jpglen > 0) {
    // read 32 bytes at a time;
    uint8_t *buffer;
    uint8_t bytesToRead = min(32, jpglen); // change 32 to 64 for a speedup but may not work with all setups!
    buffer = cam.readPicture(bytesToRead);
    int encodedStringLength = base64_enc_len(bytesToRead);
    char encodedStringBuffer[encodedStringLength];
    encodedStringLength = base64_encode(encodedStringBuffer, (char *)buffer, bytesToRead);
    jpglen -= bytesToRead;
    
  }

    String ImageFileContentsValue = String(encodedStringBuffer);
    UploadChoreo.addInput("URL", ImageFileContentsValue);

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

