#include <IRremote.h>

IRsend irsend;
int pushButton = 7; //push button connected to digital pin 7
int val = 0;     // variable for reading the button status

unsigned int  rawData[69] = {47536, 4700,4250, 750,1500, 700,1500, 700,1550, 700,400, 700,400, 700,400, 700,450, 650,450, 650,1600, 600,1600, 650,1600, 600,500, 600,500, 600,550, 600,500, 600,500, 600,1650, 550,1650, 600,1650, 550,550, 550,600, 500,600, 500,600, 550,550, 550,600, 500,600, 500,600, 500,1750, 500,1700, 500,1750, 500,1700, 500,1750, 500,0};  // SAMSUNG E0E0E01F

void setup()
{
  pinMode(pushButton, INPUT);
}

void loop() {

  val = digitalRead(pushButton);  // read input value
  if (val == HIGH) { 
  	for (int i = 0; i < 3; i++) {
      irsend.sendRaw(rawData,69,32);
  		delay(40);
  	}
  }
}
