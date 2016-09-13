#include <LiquidCrystal.h>

int pin = 2;
volatile unsigned int pulse;
float volume = 0;
float flow_rate = 0;
const int pulses_per_litre = 450;

// initialize the library with the numbers of the interface pins
LiquidCrystal lcd(8, 7, 6, 5, 4, 3);

void setup()
{
   Serial.begin(9600);
   pinMode(pin, INPUT);
   attachInterrupt(0, count_pulse, RISING);
   
   // set up the LCD's number of columns and rows: 
   lcd.begin(16, 2);
   // Print a message to the LCD.
   lcd.print("Welcome");
}

void loop()
{
  pulse = 0;
  
  interrupts();
  delay(100);
  noInterrupts();
  
  lcd.setCursor(0, 0);
  
  lcd.print("Pulses/s: ");
  lcd.print(pulse);
  
  flow_rate = pulse*1000/pulses_per_litre;
  
  lcd.setCursor(0, 1);
  lcd.print(flow_rate,2);//display only 2 decimal places
  lcd.print(" ml/s");
  
  volume = volume + flow_rate * 0.1;
  
  lcd.setCursor(7, 1);
  lcd.print(volume, 2);//display only 2 decimal places
  lcd.println(" ml");
}

void count_pulse()
{
  pulse++;
}

