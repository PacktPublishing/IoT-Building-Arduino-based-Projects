
void setup()
{

  pinMode(5, OUTPUT);
  Serial.begin(9600);  // initialize serial communications at 9600 bps
}

void loop()
{
  while (!Serial.available()) {}
  // serial read section
  while (Serial.available())
  {
    if (Serial.available() > 0)
    {
      char c = Serial.read();  //gets one byte from serial buffer

      if (c == '1')
      {
        digitalWrite(5,HIGH);
      }else if (c == '0')
      {
        digitalWrite(5,LOW);
      }
    }
  }
  
  delay(500);
  
}
