int pin = 2;
volatile unsigned int pulse;
const int pulses_per_litre = 450;

void setup()
{
     Serial.begin(9600);
     
     pinMode(pin, INPUT);
     attachInterrupt(0, count_pulse, RISING);
}

void loop()
{
    pulse = 0;
    
    interrupts();
    delay(100);
    noInterrupts();
    
    Serial.print("Pulses per second: ");
    Serial.println(pulse);
    
    Serial.print("Water flow rate: ");
    Serial.print(pulse * 1000/pulses_per_litre);
    Serial.println("milliliters per second");
}

void count_pulse()
{
    pulse++;
}

