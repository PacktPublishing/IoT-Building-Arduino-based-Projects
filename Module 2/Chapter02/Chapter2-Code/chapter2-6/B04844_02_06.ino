#include <WiFi.h>
#include <Wire.h>
#include <SPI.h>
#include "Adafruit_DRV2605.h"

Adafruit_DRV2605 drv;


char ssid[] = "yourNetwork"; 
char pass[] = "secretPassword"; 
int status = WL_IDLE_STATUS; 

void setup() 
{
  Serial.begin(9600); 
  while (!Serial) 
  {
    ; 
  }
  
  // check for the presence of the shield:
  if (WiFi.status() == WL_NO_SHIELD) {
    Serial.println("WiFi shield not present"); 
    // don't continue:
    while(true);
  } 
  
  while ( status != WL_CONNECTED) { 
    Serial.print("Attempting to connect to WPA SSID: ");
    Serial.println(ssid);
 
    status = WiFi.begin(ssid, pass);

    delay(10000);
  }
   
  Serial.print("You're connected to the network");

  drv.begin();
  drv.selectLibrary(1);
  
  drv.setMode(DRV2605_MODE_INTTRIG);
}

void loop() 
{

  if (WiFi.status() != WL_CONNECTED) 
  { 
    Serial.println("Couldn't get a wifi connection");
    while(true);
  } 
  else 
  {
  long rssi = WiFi.RSSI();
  Serial.print("Signal Strenght: ");
  Serial.println(rssi);
  Serial.print(" dBm");
  
  int range = map(rssi, -100, 0, 1, 10);
  
  switch (range) 
  {
    case 1:
    // set the effect to play
    drv.setWaveform(0, 10);  // play double click - 100% 
    drv.setWaveform(1, 0);   // end waveform
    drv.go(); // play the effect!
    break;
    
    case 2:
    // set the effect to play
    drv.setWaveform(0, 9);  // play Soft Bump - 30% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
    
    case 3:
    // set the effect to play
    drv.setWaveform(0, 8);  // play Soft Bump - 60% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
  
    case 4:
    // set the effect to play
    drv.setWaveform(0, 7);  // play Soft Bump - 100% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
  
    case 5:
    // set the effect to play
    drv.setWaveform(0, 6);  // play Sharp Click - 30% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
    
    case 6:
    // set the effect to play
    drv.setWaveform(0, 5);  // play Sharp Click - 60% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
    
    case 7:
    // set the effect to play
    drv.setWaveform(0, 4);  // play Sharp Click - 100% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
    
    case 8:
    // set the effect to play
    drv.setWaveform(0, 3);  // play Strong Click - 100% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
    
    case 9:
    // set the effect to play
    drv.setWaveform(0, 2);  // play Strong Click - 60% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
    
    case 10:
    // set the effect to play
    drv.setWaveform(0, 1);  // play Strong Click - 100% 
    drv.setWaveform(1, 0);  // end waveform
    drv.go(); // play the effect!
    break;
  
  }

 }
  delay(2000);
}

