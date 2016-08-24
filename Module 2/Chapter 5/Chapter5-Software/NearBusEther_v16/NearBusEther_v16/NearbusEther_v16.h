///////////////////////////////////////////////////////////////////////////////////////////
// NEARBUS LIBRARY - www.nearbus.net
// Description: NearBus Agent Library Header
// Platform:    Arduino
// Status:      Alpha Release
// Author:      F. Depalma
// Support:     info@nearbus.net
//
/////////////////////////////////////////////////////////////////////////////////////////// 

///////////////////////////////////////////////////////////////////////////////////////////
//  NEARBUS INTERNET CONFIGURATION   
///////////////////////////////////////////////////////////////////////////////////////////
#define NEARBUS_API		"/v1/ardu_hub_v14.html" 														// NearBus API Service
#define NEARBUS_IP		46, 252, 193, 124																// VPS

#if defined( ARDUINO_ETHER )			
#include <Ethernet.h>																					// Ether Specific Configuration
#endif
#if defined( ARDUINO_WIFI )			
#include <WiFi.h>	
#endif
#if defined( ARDUINO_YUN )			
#include <Bridge.h>
#include <YunClient.h>																					
#endif


/////////////////////////////////////////////////////////////////////////////////////////////////////
// DEFINES / GLOBAL VARIABLES
/////////////////////////////////////////////////////////////////////////////////////////////////////
#define  DEBUG_DATA  	 1																				// Data Rx / Tx Debug
#define  DEBUG_BETA   	 0																				// Beta Debug
#define  DEBUG_ERROR  	 0																				// Error Messages
	
#define CHANNELS_NUMBER  4																				// 4 Channel Support 


///////////////////////////////////////////////////////////////////////////////////////////
//  NEARNUS PIN-OUT CONFIGURATION
//  (uncomment only one option) 
///////////////////////////////////////////////////////////////////////////////////////////

#define	STDR_PINOUT 					// Pin-Out for EThernet & Wi-Fi Shield (Pins 3-5-6-9, supports PWM in all pins)
// #define	RELAY_PINOUT 				// Pin-Out for Relay Shield SeeedStudio (Pins 4-5-6-7)
// #define	GPRS_PINOUT 				// Pin-Out for GPRS Shield (Pins 5-6-7-10, the GPRS Shield uses the pins 3 and 4 )

#if defined( STDR_PINOUT )			
#define DIG_PORT_0		3				// Pin Out for GPRS Shield
#define DIG_PORT_1		5				// Pin Out for GPRS Shield
#define DIG_PORT_2		6				// Pin Out for GPRS Shield
#define DIG_PORT_3		9				// Pin Out for GPRS Shield
#endif

#if defined( RELAY_PINOUT )
#define DIG_PORT_0		4				// Pin Out for RELAY Shield
#define DIG_PORT_1		5				// Pin Out for Relay Shield
#define DIG_PORT_2		6				// Pin Out for Relay Shield
#define DIG_PORT_3		7				// Pin Out for Relay Shield
#endif

#if defined( GPRS_PINOUT )
#define DIG_PORT_0		5				// Pin Out for Ether/WiFi Shield
#define DIG_PORT_1		6				// Pin Out for Ether/WiFi Shield
#define DIG_PORT_2		9				// Pin Out for Ether/WiFi Shield
#define DIG_PORT_3		10				// Pin Out for Ether/WiFi Shield
#endif

#define ADC_PORT_0		0				// Analog Channel_0
#define ADC_PORT_1		1				// Analog Channel_1
#define ADC_PORT_2		2				// Analog Channel_2
#define ADC_PORT_3		3				// Analog Channel_3

#define RMS_THRESHOLD	3				// RMS ADC THRESHOLD 

#define NEAR_LED  		8				// NearBus activity LED indicator

#define FLEXI_TIMER		1				// 1=FlexiTimer Active
#define INT_PERIOD		5				// Interrupt Period in [ms]


#define	GET_MODE		1
#define	POST_MODE		2

///////////////////////////////////////////////////////////////////////////////////////////
//  END OF CUSTOMER CONFIGURATION
///////////////////////////////////////////////////////////////////////////////////////////
#define HEX_FORMAT		"r%u:x%08lx"		// Define the Debug Service Format[HEX]
#define DEC_FORMAT		"r%u:d%lu"			// Define the Debug Parameter Format[UINT]


#ifndef Nearbus_h
#define Nearbus_h

#include <WProgram.h>
#include <SPI.h>
#include <Servo.h>

#if FLEXI_TIMER	
	#include <FlexiTimer2.h>
#endif

// ADC DEFINITION FOR ARDUINO MEGA
#if defined(__AVR_ATmega1280__) || defined(__AVR_ATmega2560__)
#define _INTERNAL 		INTERNAL1V1
#else
#define _INTERNAL 		INTERNAL
#endif


// TYPE DEFINITION
#define UINT unsigned int
#define ULONG unsigned long


////////////////////////////////////////////
// Variables Estados Puertos
////////////////////////////////////////////
#define  RESET_MODE     0
#define  OUTPUT_MODE    1
#define  INPUT_MODE     2
#define  PWM_MODE       3
#define  PULSE_MODE     4
#define  TEST_MODE      5
#define  ADC_MODE      	6
#define  FULL_PWM_MODE  7
#define  DIG_COUNT_MODE 8
#define  RMS_MODE      	9
#define  TRIGGER_MODE  	10
#define  MYNBIOS_MODE	11
#define  DONE_MODE     	12
#define  ACCUMUL_MODE	14

struct PRT_CNTRL_STRCT { 	                                              								// VMCU Control Structure
  byte  pinId;                                                                  						// Internal port number of MCU
  byte  anaPinId;																						// Arduino ADC pin 
  byte  portMode;                                                                						// MCU PIN mode configuration
  byte  lastDigitalValue;                                                        						// Last Digital Port Value (t-1)
  ULONG pulseCounter;
  ULONG portValue;																						// Actual Port Value (16 bits) 
  ULONG setValue;                                                              							// Port SetedValue (Pulse duration (in 10ms steps), PWM value, etc
};


class Nearbus {
 
  public:
	Nearbus(int init);	
	void NearChannel( ULONG*, ULONG*, int* );
	void NearInit( char* deviceId, char* sharedSecret );
	void PortServices (void);
	
  private:
	void MakePost();
	int  ReadData();
	char ReadChar( void );	
	void PrintString( void );	
	
	void NearBiosMainSwitch( UINT, ULONG, ULONG*, byte );
	void AgentReset( void );

	void PortModeConfig( byte, byte );	

	void ReadAdcPort( byte, ULONG, ULONG*, byte );														// Implemented in v0.1
	void ReadDigitalPort( byte, ULONG, ULONG*, byte );													// Implemented in v0.1
	void RmsInput( byte, ULONG, ULONG*, byte );															// Implemented in v0.8

	void ResetPort ( byte, ULONG, ULONG*, byte );														// Implemented in v0.8
	void TriggerInput ( byte, ULONG, ULONG*, byte );													// Implemented in v0.8
	void WriteDigitalPort ( byte, ULONG, ULONG*, byte );												// Implemented in v0.1
	void PwmOutput( byte, ULONG, ULONG*, byte );														// Implemented in v0.1
	void FullPwmOutput( byte, ULONG, ULONG*, byte );													// To be Implemented	
	void PulseOutput( byte, ULONG, ULONG*, byte );														// Implemented in v0.6
	void DigitalCounter( byte, ULONG, ULONG*, byte );													// Implemented in v0.8
	void DigitalAccumulator( byte, ULONG, ULONG*, byte );												// Implemented in v0.8
	
	void MyNbios_0( byte, ULONG, ULONG*, byte, PRT_CNTRL_STRCT* );										// Implemented in v0.7
	void MyNbios_1( byte, ULONG, ULONG*, byte, PRT_CNTRL_STRCT* );										// Implemented in v0.7

};


#endif



