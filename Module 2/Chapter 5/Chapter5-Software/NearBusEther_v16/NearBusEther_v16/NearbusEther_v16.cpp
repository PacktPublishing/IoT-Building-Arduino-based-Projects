/////////////////////////////////////////////////////////////////////////////////////////////////////
// NEARBUS LIBRARY - www.nearbus.net
// Description: NearBus Agent Main Library
// Platform:    Arduino
// Status:      Alpha Release
// Author:      F. Depalma
// Support:     info@nearbus.net
//
/////////////////////////////////////////////////////////////////////////////////////////////////////
#define	ARDUINO_ETHER
//#define	ARDUINO_WIFI
//#define	ARDUINO_YUN

/////////////////////////////////////////////////////////////////////////////////////////////////////
char server[] = "nearbus.net";																			// DNS Support
//IPAddress server ( NEARBUS_IP ); 																		// IP Static

#if defined ARDUINO_ETHER			
#include <NearbusEther_v16.h> 																			// [REL]																			
EthernetClient client;																					// Ether Client
#endif
#if defined ARDUINO_WIFI			
#include <NearbusWiFi_v16.h> 																			// [REL]		 
WiFiClient client;																						// WiFi Client
#endif
#if defined ARDUINO_YUN			
#include <NearbusYun_v16.h>	 																			// [REL]		
YunClient client;  																						// Yun Client
#endif	


/**************************************************************************************************************************************
 *  NEARBIOS VARIABLES 																											  	  *
 **************************************************************************************************************************************/
Servo nearServo;

ULONG last_millis_sample;

ULONG counterInitialTime;																				// Period (initial time reference) 32bits
byte  eventCounter;																						// Event accumulator 
int   frequencyValue;
byte  setInterrupt = 0;

struct PRT_CNTRL_STRCT portControlStruct[CHANNELS_NUMBER] = {0};

//********************************
// SyncTimeBase Variables
//********************************
ULONG serverReferenceTime;
ULONG offsetTime;


/**************************************************************************************************************************************
 * 	NEARCHANNEL VARIABLES 																										 	  *
 **************************************************************************************************************************************/
char deviceName[10] = {0};
char deviceSignature[10] = {0};

ULONG txSequenceId = 60;
ULONG txSeqAck;
ULONG txCommand;

char rxDeviceName[9];																					// 8 char Device ID
char rxSignature[9];																					// 8 char Signature	
ULONG rxSequenceId;																						// 32 bits (header)
ULONG rxSeqAck;																							// 32 bits (header)
ULONG rxCommand;																						// 32 bits (header)
ULONG rxPoolingDelay; 																					// 32 bits (header)
ULONG rxServerDelay;																					// 32 bits (header) 
ULONG rxDataExchange;																					// 32 bits (header)

ULONG rxTxBuffer[8];																					// 32 bits (data)

ULONG fullDataExchange;

ULONG scheduleDelay;
ULONG poolingDelay = 2000;																				// Default (5000 ms)

byte rxNearMode;
byte rxRemoteDebug = 1;
byte ready = 0;
byte hubDataRxError = 0;                                                                                // Seted to indicate error in data received from the NearHUB

char auxData[30];																						// 2^32=>10char+1(0x0A)	

Nearbus::Nearbus(int init) {}																			// Constructor


/*####################################################################################################################################
#######################################################################################################################################
###																																	###
###		COMMUNICATIONS MODULE																										###
###																																	###
#######################################################################################################################################
 ####################################################################################################################################*/

/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearChannel Function: ReadChar( )													
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
char Nearbus::ReadChar( )
{
int i;
char c;
	  for( i=0 ; i< 300 ; i++ )
	  {
		if ( client.available() ) {
			c = client.read();
			return ( c );
		}
		delay(10);
	  }	
	  return(0xFF);
}


/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearChannel Function: PrintString( )				                                                             
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
void Nearbus::PrintString( void )
{
int i; 
int len;
	len = strlen( auxData );
	//--------------------------------------------------------------------	
	#if DEBUG_ERROR
		if( len > 30 ) { Serial.println("ERROR> PrintString() Buffer Overflow" );	}
	#endif
	//--------------------------------------------------------------------			
	#if defined ARDUINO_ETHER || defined  ARDUINO_WIFI	
	client.print( auxData );
	#endif
	#if defined ARDUINO_YUN
	for( i=0 ; i< len ; i++ ) {
		client.write( auxData[i] );
	}
	#endif
}


/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearChannel Function: Cloud Data Tx Routine				                                                             
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
void Nearbus::MakePost(void)
{
char auxQuotes[2] = {0};
ULONG auxIniTime;   
int i;
int lenght;
ULONG regNull = 0;

	auxIniTime = millis();  
	
	if ( client.connect( server, 80 ) )
    {          
		///// Payload Lenght Calculation /////
		lenght = 16;											// 8+8 = dev_name+dev_sig	
		sprintf( auxData, "%lu", txSequenceId );
		lenght += strlen( auxData );
		sprintf( auxData, "%lu", txSeqAck );
		lenght += strlen( auxData );
		sprintf( auxData, "%lu", txCommand );
		lenght += strlen( auxData );
		sprintf( auxData, "%lu", regNull );
		lenght += strlen( auxData );
		sprintf( auxData, "%lu", regNull );
		lenght += strlen( auxData );
		sprintf( auxData, "%lu", fullDataExchange );
		lenght += strlen( auxData );

		for( i=0 ; i< 8 ; i++ ) {
			sprintf( auxData, "%lu", rxTxBuffer[i] );	
			lenght += strlen( auxData );
		}			
		lenght += 16;  											// 16 * 0x0A 		
		
		sprintf( auxData, "POST " );
		PrintString( );
		sprintf( auxData, NEARBUS_API );
		PrintString( );
		sprintf( auxData, " HTTP/1.0\r\n" );            	
		PrintString( );
		sprintf( auxData, "Host: nearbus.net\r\n" );
		PrintString( );
		sprintf( auxData, "Content-Type: text/html\r\n");
		PrintString( );
		sprintf( auxData, "Content-Length: " );
		PrintString( );
		sprintf( auxData, "%d\r\n\r\n", lenght );									
		PrintString( );
		
		sprintf( auxData, deviceName );								// [0] DeviceName (ID)
		strcat( auxData, "\n" );
		PrintString( );		
		sprintf( auxData, deviceSignature );						// [1] Shared Secret
		strcat( auxData, "\n" );
		PrintString( );
		
		sprintf( auxData, "%lu\n", txSequenceId );  				// [2] Packet Sequence
		PrintString( );		
		sprintf( auxData, "%lu\n", txSeqAck );  					// [3] Sequence ACK
		PrintString( );			
		sprintf( auxData, "%lu\n", txCommand );						// [4] Command Executed 
		PrintString( );		
		sprintf( auxData, "%lu\n", regNull );  						// [5] Pooling Period (N/A)
		PrintString( );	
		sprintf( auxData, "%lu\n", regNull );  						// [6] HUB Delay Offset	
		PrintString( );		
		sprintf( auxData, "%lu\n", fullDataExchange ); 				// [7] Total Data Exchange (Rx+Tx)	 
		PrintString( );					
		
		for( i=0 ; i< 8 ; i++ ) {			
			sprintf( auxData, "%lu\n", rxTxBuffer[i] ); 			// [8-15] Registers (x8)
			PrintString( );		
		}	
	}	
    else
	{
		//--------------------------------------------------------------------	
		#if DEBUG_ERROR
			Serial.println("ERROR> Connection failed");
		#endif
		//--------------------------------------------------------------------		
	}	
}


/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearChannel Function: Parsing Rx Data
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
int Nearbus::ReadData(void) 
{
int  i;
int  j;
int  len; 
char auxData[11];
ULONG rxHeader[6];


    len = client.available();
	
	//--------------------------------------------------------------------	
	   #if DEBUG_BETA
			Serial.print("DEBUG> Rx Data Available = ");
			Serial.println( len );			
	   #endif
	//--------------------------------------------------------------------	
	if ( len )
	{ 
		hubDataRxError = 1;																				// default setting
		ready = 0;																						

		///////////////////////////////
		// Searching Start Tag
		///////////////////////////////
		i=0;
		while( 1 )
		{	
			i++;
			auxData[0] = ReadChar( );
			if( auxData[0] == 0xFF ) {
				return(1);
			}
			if( auxData[0] == 'D' ) {
				i++;
				auxData[0] = ReadChar( );
				if( auxData[0] == 0xFF ) {
					return(1);
				}					
				if ( auxData[0] == 'A' ) {
					i++;
					auxData[0] = ReadChar( );
					if( auxData[0] == 0xFF ) {
						return(1);
					}					
					if( auxData[0] == 'T' ) {
						i++;
						auxData[0] = ReadChar( );
						if( auxData[0] == 0xFF ) {
							return(1);
						}						
						if ( auxData[0] == 'A' ) {
							break;
						}
					}	
				}
			}
			if( i > 400 ) {
				return (1);
			}
		}
		
		//// New Line ////			
		if( ReadChar( ) != 0x0A ) {
			return (1);
		}
		
		//// DEVICE_IDENTIFIER ////
		for ( i=0 ; i<9; i++ )	{
			auxData[i] = ReadChar( );
			if( auxData[i] == 0xFF) {
				return (1);			
			}	
			if ( auxData[i] == 0x0A ) {
				auxData[i] = 0x00;			
				break;
			}
		}
		if( i == 9 ) {
			return (1);
		}
		strcpy( rxDeviceName, auxData );	
		
		//// SIGNATURE ////
		for ( i=0 ; i<9; i++ )	{
			auxData[i] = ReadChar( );
			if( auxData[i] == 0xFF) {
				return (1);			
			}	
			if ( auxData[i] == 0x0A ) {
				auxData[i] = 0x00;			
				break;
			}
		}
		if( i == 9 ) {
			return (1);
		}
		strcpy( rxSignature, auxData );	
		
		//// HEADERS ////
		for( j=0 ; j<6 ; j++ )
		{
			for ( i=0 ; i<11; i++ )	{
				auxData[i] = ReadChar( );
				if( auxData[i] == 0xFF) {
					return (1);			
				}					
				if ( auxData[i] == 0x0A ) {
					auxData[i] = 0x00;			
					break;
				}
			}
			if( i == 11 ) {
				return (1);
			}
			rxHeader[j] = atol( auxData );
		}
		rxSequenceId     	= rxHeader[0];
		rxSeqAck		    = rxHeader[1];                
		rxCommand		    = rxHeader[2];                
		rxPoolingDelay   	= rxHeader[3]; 
		rxServerDelay     	= rxHeader[4]; 		
		rxDataExchange     	= rxHeader[5]; 	
		
		rxNearMode 			= (byte)( rxCommand & 0x000000FF );
		rxRemoteDebug 		= (byte)( ( rxCommand >> 8 ) & 0x00000FF );		
		
		//// REGISTERS ////
		for( j=0 ; j<8 ; j++ )
		{
			for ( i=0 ; i<11; i++ )	{
				auxData[i] = ReadChar( );
				if( auxData[i] == 0xFF) {
					return (1);			
				}					
				if ( auxData[i] == 0x0A ) {
					auxData[i] = 0x00;			
					break;
				}
			}
			if( i == 11 ) {
				return (1);
			}
			rxTxBuffer[j] = atol( auxData );
		}		
	
		ready = 1;
		hubDataRxError = 0;		
	}
	return(0);
}




/*####################################################################################################################################
#######################################################################################################################################
###																																	###
###		NEARBIOS_MODULE (VMCU)																										###
###																																	###
#######################################################################################################################################
 ####################################################################################################################################*/


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Reconfiguring Ports (ADC or I/O)
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::PortModeConfig( byte portId, byte mode)
{

	if(  mode !=PWM_MODE && portControlStruct[portId].portMode == PWM_MODE ) {
		if( nearServo.attached( ) ) {	
			nearServo.detach( );
		}			
	}
	
	portControlStruct[portId].portMode = mode;
	
	switch( mode )
	{

		case RESET_MODE:
			pinMode( portControlStruct[portId].pinId, INPUT );											// Configuring the port as Dig_In (High Z)    
			break;
		
		case INPUT_MODE:
		case TRIGGER_MODE:			
			pinMode( portControlStruct[portId].pinId, INPUT );											// Configuring the port as Dig_In (High Z)    
			break;		
	
		case DIG_COUNT_MODE:
		case ACCUMUL_MODE:
			pinMode( portControlStruct[portId].pinId, INPUT_PULLUP );									// Port as Dig_In + 20K Pullup resistor  
			break;		
	
		case PWM_MODE:	
			nearServo.attach( portControlStruct[portId].pinId );
			break;
			
		case FULL_PWM_MODE:	
			// void
			break;		
		
		case OUTPUT_MODE:		
		case PULSE_MODE:
			pinMode( portControlStruct[portId].pinId, OUTPUT );											// Configuring the port as Dig Out      
			break;
		
		case ADC_MODE:
		case RMS_MODE:
			pinMode( portControlStruct[portId].pinId, INPUT );		
			break;	
			
		case MYNBIOS_MODE:
			// void
			break;
			
		default:
			break;	
	}
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: DeviceTest
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::AgentReset(  )
{
	// To Implement
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Reset Port
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::ResetPort( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{
	
	*pRetValue = 0x0; 
		
	if( vmcuRxMethod == POST_MODE )
	{	
		PortModeConfig( portId, RESET_MODE );

		portControlStruct[portId].pulseCounter 	   = 0;
		portControlStruct[portId].lastDigitalValue = 0;
		portControlStruct[portId].portValue 	   = 0;
		portControlStruct[portId].setValue 		   = 0;		
		
		*pRetValue = 0x0; 	
	}	
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Read Digital Port
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::ReadDigitalPort( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{ 	

	*pRetValue = 0x0;
		
	if( portControlStruct[portId].portMode != INPUT_MODE )
	{
		PortModeConfig( portId, INPUT_MODE );	
	}
	
	if( vmcuRxMethod == GET_MODE )
	{		 
		* pRetValue = digitalRead( portControlStruct[portId].pinId );
	}	
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Read ADC
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::ReadAdcPort( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{ 
int	adcChannel;

	*pRetValue = 0x0; 	
		
	if( portControlStruct[portId].portMode != ADC_MODE )
	{
		PortModeConfig( portId, ADC_MODE );																// Configuring the port as Analog		
		analogReference( DEFAULT );																		// 5000mV or 3300mV
	}
	
	if( vmcuRxMethod == GET_MODE )
	{
		* pRetValue = analogRead( portControlStruct[portId].anaPinId );
	}
	else if( vmcuRxMethod == POST_MODE )
	{		
		if( rxValue == 1100 ) {
			analogReference( _INTERNAL );																// 1100mV Default
		}
		else {
			analogReference( DEFAULT );																	// 5000mV or 3300mV
		}		
	}		
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Trigger Input
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::TriggerInput( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{ 

	*pRetValue = 0x0; 	
		
	if( portControlStruct[portId].portMode != TRIGGER_MODE )
	{
		PortModeConfig( portId, TRIGGER_MODE );															// Configuring the port as Analog
	}
	
	if( vmcuRxMethod == GET_MODE )
	{	
		*pRetValue = portControlStruct[portId].portValue;   											// it returns the port value
		portControlStruct[portId].portValue = 0;
	}
	else if( vmcuRxMethod == POST_MODE )
	{		
		portControlStruct[portId].portValue = 0;
		portControlStruct[portId].lastDigitalValue	= 0;	
		portControlStruct[portId].setValue = rxValue;
		*pRetValue = 0x0; 	
	}
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Write Digital Port
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::WriteDigitalPort( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{

	*pRetValue = 0x0; 	
		
	if( portControlStruct[portId].portMode != OUTPUT_MODE )
	{
		PortModeConfig( portId, OUTPUT_MODE );	
	}
	
	if( vmcuRxMethod == GET_MODE )
	{	
		*pRetValue = portControlStruct[portId].portValue;   											// it returns the port value
	}
	else if( vmcuRxMethod == POST_MODE )
	{	
		if( rxValue == 0 )
		{ 
			digitalWrite(  portControlStruct[portId].pinId, LOW );
			portControlStruct[portId].portValue = 0;		
		}
		else
		{
			digitalWrite(  portControlStruct[portId].pinId, HIGH ); 
			portControlStruct[portId].portValue = 1;		
		}
		*pRetValue = rxValue;	
	}
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Write Digital Pulse
/////////////////////////////////////////////////////////////////////////////////////////////////////  
void Nearbus::PulseOutput( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{
	
	*pRetValue = 0x0; 	
	
	if( portControlStruct[portId].portMode != PULSE_MODE )
	{
		PortModeConfig( portId, PULSE_MODE );	
	}
	
	if( vmcuRxMethod == GET_MODE )
	{	
		*pRetValue = portControlStruct[portId].portValue;   											// it returns the port value
	}
	else if( vmcuRxMethod == POST_MODE )
	{
		portControlStruct[portId].setValue = (rxValue/INT_PERIOD)*INT_PERIOD;

		if( rxValue == 0 ){ 
			digitalWrite( portControlStruct[portId].pinId, LOW );
			portControlStruct[portId].portValue = 0;
		}
		else {
			digitalWrite( portControlStruct[portId].pinId, HIGH ); 
			portControlStruct[portId].portValue = 1;
		}
		*pRetValue = rxValue;	
	}
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: Servo Output 
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::PwmOutput( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{

	*pRetValue = 0x0; 
	
	if( portControlStruct[portId].portMode != PWM_MODE )
	{
		PortModeConfig( portId, PWM_MODE );
	}
	
	if( vmcuRxMethod == GET_MODE )
	{	
		*pRetValue = portControlStruct[portId].portValue;   											// it returns the port value
	}	
	else if( vmcuRxMethod == POST_MODE )
	{
		if( rxValue > 2200 ) {
			rxValue = 2200;																			// Value in microseconds
		}
		else if( rxValue < 800 ) {
			rxValue = 800;																				// Value in microseconds
		}
		portControlStruct[portId].setValue = rxValue;
		nearServo.writeMicroseconds( (UINT) rxValue );
		*pRetValue = rxValue;
	}	
}


/////////////////////////////////////////////////////////////////////////////////////////////////////
// NearBIOS Function: PWM Output 
/////////////////////////////////////////////////////////////////////////////////////////////////////
void Nearbus::FullPwmOutput( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{
#if !FLEXI_TIMER	

int auxPort;

	*pRetValue = 0x0; 

	if( portControlStruct[portId].portMode != FULL_PWM_MODE )
	{
		PortModeConfig( portId, FULL_PWM_MODE );
	}
	if( vmcuRxMethod == GET_MODE )
	{	
		*pRetValue = portControlStruct[portId].portValue;   											// it returns the port value
	}	
	else if ( vmcuRxMethod == POST_MODE )
	{
		if( rxValue > 255 )
		{
			rxValue = 255;																				// Value in microseconds
		}
		portControlStruct[portId].setValue = rxValue;
		auxPort = (int) portControlStruct[portId].pinId;
		analogWrite( auxPort, (int) rxValue ); 
		*pRetValue = rxValue;
	}
	
#endif
}

 
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearBIOS Function: Digital Counter
/////////////////////////////////////////////////////////////////////////////////////////////////////   //  
void Nearbus::DigitalCounter( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{
	
	*pRetValue = 0x0; 	

	if ( vmcuRxMethod == GET_MODE )
	{
		if( portControlStruct[portId].portMode == DONE_MODE )
		{
			*pRetValue = portControlStruct[portId].pulseCounter; 		
			portControlStruct[portId].pulseCounter = 0;
			portControlStruct[portId].portValue = ( portControlStruct[portId].setValue/INT_PERIOD ) * INT_PERIOD;
			PortModeConfig( portId, DIG_COUNT_MODE );		
		}
	}
	else if( vmcuRxMethod == POST_MODE )
	{ 	
		if( portControlStruct[portId].portMode != DIG_COUNT_MODE )
		{
			PortModeConfig( portId, DIG_COUNT_MODE );	
		}			
		portControlStruct[portId].setValue = rxValue;
		portControlStruct[portId].pulseCounter = 0;
		portControlStruct[portId].portValue = ( rxValue/INT_PERIOD ) * INT_PERIOD;		
	}
}


 
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearBIOS Function: Digital Accumulator
/////////////////////////////////////////////////////////////////////////////////////////////////////   //  
void Nearbus::DigitalAccumulator( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{
	
	*pRetValue = 0x0; 	
	
	if( portControlStruct[portId].portMode != ACCUMUL_MODE )
	{
		PortModeConfig( portId, ACCUMUL_MODE );				
	}	
	
	if ( vmcuRxMethod == GET_MODE )
	{
		*pRetValue = portControlStruct[portId].pulseCounter; 		
	}
	else if( vmcuRxMethod == POST_MODE )
	{ 
		portControlStruct[portId].portValue = 0;
		portControlStruct[portId].pulseCounter = 0;
		portControlStruct[portId].setValue = 0;				
	}
}
		

 
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearBIOS Function: Agent Service Manager
/////////////////////////////////////////////////////////////////////////////////////////////////////   // 
void Nearbus::RmsInput( byte portId, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{
unsigned long sensorValue;
unsigned long rmsValue;
unsigned long rmsAverange;
int samples;
int n, m;
int wait;
byte state;
byte done;
byte auxPinId;
float scale;

	*pRetValue = 0x0; 	
		
	if( portControlStruct[portId].portMode != RMS_MODE )
	{
		PortModeConfig( portId, RMS_MODE );																// Configuring the port as Analog		
		analogReference( DEFAULT );																		// 5000mV or 3300mV
		portControlStruct[portId].setValue = 5000;
	} 
	
	if( vmcuRxMethod == GET_MODE )
	{
		auxPinId = portControlStruct[portId].anaPinId; 	
		rmsAverange = 0;
		scale = portControlStruct[portId].setValue/1023;
		
		for( m=0; m<5; m++) 
		{	
			wait = 0;
			state = 0;	
			done = 0;

			for(n=0; n<500; n++) 
			{
				sensorValue = (unsigned long) analogRead(auxPinId);

				switch( state )
				{
				  case 0: 
					if( sensorValue < RMS_THRESHOLD ) {
					  wait++;
					}
					if( wait > 50 ) {
					  state = 1;		  
					}
					break;		
				
				  case 1:
					if( sensorValue > RMS_THRESHOLD ) {
						rmsValue = sensorValue * sensorValue;
						samples = 1;
						state = 2;
					}
					else {
						wait++;
						if ( wait > 400 ) {
							*pRetValue = 0;
							return;
						}
					}					
					break;
					
				  case 2:																				 // each interaction takes 8.5uS 
					//digitalWrite( 3, HIGH );  
					rmsValue += sensorValue * sensorValue;
					samples++;
					if( sensorValue < RMS_THRESHOLD ) {
					  done = 1;
					}     
					//digitalWrite( 3, LOW );   			
					break;
					
				}
				if( done ) {
					rmsAverange += (ULONG) (sqrt(rmsValue/samples) * scale );
					break;
				}
			} 
		}
		*pRetValue = rmsAverange / 5;
	}
	else if( vmcuRxMethod == POST_MODE )
	{ 	
		if( rxValue == 1100 )
		{	
			analogReference( _INTERNAL );																// 1100mV Default
			portControlStruct[portId].setValue = 1100;
		}
		else
		{
			analogReference( DEFAULT );																	// 5000mV or 3300mV
			portControlStruct[portId].setValue = 5000;
		}
	}	
}


 
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearBIOS Function: Agent Service Manager
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
void Nearbus::NearBiosMainSwitch( UINT auxIndex, ULONG rxValue, ULONG* pRetValue, byte vmcuRxMethod )
{
byte portId;
	
	switch ( auxIndex )
	{        
		//************************************
		// NOP
		//************************************ 
		case 0x0000:
			break;  

			
		//************************************
		// Device Reset
		//************************************ 
		case 0x000F:
			AgentReset( );
			break;  			
			
			
		default:
			// void
			break;
	} 
	
	portId = byte (auxIndex & 0x000F);
	if ( portId < 8 )
	{
		switch ( (auxIndex & 0xFFF0) )
		{   	
			//************************************
			// [0x0010] - Digital Inputs
			//************************************
			case 0x0010:
				ReadDigitalPort( portId, rxValue, pRetValue, vmcuRxMethod );
				break;     
				
				
			//************************************
			// [0x0020] - Digital Outputs
			//************************************    
			case 0x0020:
				WriteDigitalPort( portId, rxValue, pRetValue, vmcuRxMethod );
				break;     
				
			 
			//************************************
			// [0x0030] - Analog Inputs
			//************************************
			case 0x0030:
				ReadAdcPort( portId, rxValue, pRetValue, vmcuRxMethod );
				break; 	 
		
		
			//************************************
			// [0x0040]  Pulse Output
			//************************************
			case 0x0040: 
				PulseOutput( portId, rxValue, pRetValue, vmcuRxMethod );
				break;
			 		
		
			//************************************
			// [0x0050]  Trigger Input
			//************************************
			case 0x0050: 
				TriggerInput( portId, rxValue, pRetValue, vmcuRxMethod );
				break;
				
					
			//************************************
			// [0x0060]  PWM Output
			//************************************
			case 0x0060: 
				PwmOutput( portId, rxValue, pRetValue, vmcuRxMethod );
				break;
			 		
				
			//************************************
			// [0x0070]  Servo Output
			//************************************
			case 0x0070: 
				FullPwmOutput( portId, rxValue, pRetValue, vmcuRxMethod );
				break;			 

			//************************************
			// [0x0080]  Digital Counter
			//************************************
			case 0x0080: 
				DigitalCounter( portId, rxValue, pRetValue, vmcuRxMethod );
				break;	

				
			//************************************
			// [0x0090]  RMS_INPUT
			//************************************
			case 0x0090: 
				RmsInput( portId, rxValue, pRetValue, vmcuRxMethod );
				break;

			//************************************
			// [0x00C0]  Digital Accumulator
			//************************************
			case 0x00C0: 
				DigitalAccumulator( portId, rxValue, pRetValue, vmcuRxMethod );
				break;					
				
			//************************************
			// [0x00F0]  RESET_PORT
			//************************************
			case 0x00F0: 
				ResetPort( portId, rxValue, pRetValue, vmcuRxMethod );
				break;
				
				
			//************************************
			// [0x0200]  MY_NBIOS_0
			//************************************
			case 0x0200:
				MyNbios_0( portId, rxValue, pRetValue, vmcuRxMethod, &portControlStruct[portId] );
				break;
				
				
			//************************************
			// [0x0210]  MY_NBIOS_1
			//************************************
			case 0x0210:
				// MyNbios_1( portId, rxValue, pRetValue, vmcuRxMethod, &portControlStruct[portId] );
				break;
			
			default:
				// void
				break;
		} 
	}
}


/////////////////////////////////////////////////////////////////////////////////////////////////////   //
// NearBIOS: Port Service Routine
/////////////////////////////////////////////////////////////////////////////////////////////////////   //
void Nearbus::PortServices (void)
{
byte  i;
byte  portInput;
byte  triggerMode;

	//************************************
	// Port Services
	//************************************	
	for( i=0 ; i<CHANNELS_NUMBER ; i++ )
	{			
		//***********************************
		// PULSE MODE
		//***********************************
		if ( portControlStruct[i].portMode == PULSE_MODE )
		{
			if( portControlStruct[i].setValue < INT_PERIOD )
			{
				portControlStruct[i].setValue = 0;
				digitalWrite(  portControlStruct[i].pinId, LOW );
				portControlStruct[i].portValue = 0;
				portControlStruct[i].portMode = DONE_MODE;			
			}
			else
			{
				portControlStruct[i].setValue -= INT_PERIOD;   											// 5 ms Decrement
			}
		}	

		//***********************************
		// DIGITAL COUNTER
		//***********************************
		else if ( portControlStruct[i].portMode == DIG_COUNT_MODE )
		{
			portInput = digitalRead( portControlStruct[i].pinId );
			
			if( portControlStruct[i].lastDigitalValue == 1 &&  portInput == 0 )
			{
				portControlStruct[i].pulseCounter++;
			}
			portControlStruct[i].lastDigitalValue = portInput;
			
			portControlStruct[i].portValue -= INT_PERIOD;			
			
			if( portControlStruct[i].portValue < INT_PERIOD )
			{
				portControlStruct[i].portMode = DONE_MODE;
			}
		}
		else if ( portControlStruct[i].portMode == ACCUMUL_MODE )
		{
			portInput = digitalRead( portControlStruct[i].pinId );
			
			if( portControlStruct[i].lastDigitalValue == 1 &&  portInput == 0 )
			{
				portControlStruct[i].pulseCounter++;
			}
			portControlStruct[i].lastDigitalValue = portInput;	
		}
		
		//***********************************
		// TRIGGER INPUT
		//***********************************
		else if ( portControlStruct[i].portMode == TRIGGER_MODE )
		{
			portInput = digitalRead( portControlStruct[i].pinId );
			
			triggerMode = (byte) portControlStruct[i].setValue;												// [1]=>Rising Edge [+-0]=>Falling Edge
			
			if( portInput == triggerMode && portControlStruct[i].lastDigitalValue == !triggerMode )
			{
				portControlStruct[i].portValue = 1;
			}
			portControlStruct[i].lastDigitalValue = portInput;
		}		
	}
}




/*####################################################################################################################################
#######################################################################################################################################
###																																	###
###		NEAR_MANAGER MODULE																											###
###																																	###
#######################################################################################################################################
 ####################################################################################################################################*/

//////////////////////////////////////////////////////////////////////////                        
// NearChannel Function: Initialization
//////////////////////////////////////////////////////////////////////////  
void Nearbus::NearInit( char* deviceId, char* sharedSecret )
{
byte i;

    strcpy ( deviceName, deviceId );
    strcpy ( deviceSignature, sharedSecret );

	/////////////////////////////
	// Configura canales VMCU
	/////////////////////////////
	portControlStruct[0].pinId = DIG_PORT_0;															// Channel_0
	portControlStruct[1].pinId = DIG_PORT_1;															// Channel_1
	portControlStruct[2].pinId = DIG_PORT_2;															// Channel_2
	portControlStruct[3].pinId = DIG_PORT_3;															// Channel_3
	
	portControlStruct[0].anaPinId = ADC_PORT_0;															// Analog Channel_0
	portControlStruct[1].anaPinId = ADC_PORT_1;															// Analog Channel_1
	portControlStruct[2].anaPinId = ADC_PORT_2;															// Analog Channel_2
	portControlStruct[3].anaPinId = ADC_PORT_3;															// Analog Channel_3

	///////////////////////////
	// Set channels as INPUT
	///////////////////////////	
	pinMode( DIG_PORT_0, INPUT );	
	pinMode( DIG_PORT_1, INPUT );	
	pinMode( DIG_PORT_2, INPUT );	
	pinMode( DIG_PORT_3, INPUT );
	pinMode( NEAR_LED, OUTPUT );																		// NearBus activity LED
		
	for(i=0 ; i<4 ; i++)
	{
		digitalWrite( NEAR_LED, HIGH );
		delay(350);
		digitalWrite( NEAR_LED, LOW );
		delay(350);	
	}
	
}


/////////////////////////////////////////////////////////////////////////////////////////////////////	//
// NearChannel Function: Main API function
/////////////////////////////////////////////////////////////////////////////////////////////////////	//
void Nearbus::NearChannel( ULONG* txData, ULONG* rxData, int* ret )
{
int   i;  
byte  frameRxError = 0;
int   offsetError;
ULONG retValue;
UINT  auxService;
byte  vmcuRxMethod;

	/////////////////////////////////////////////////////////////////////////////////////////////////  	//	
	//  NearHub Communication Module
	///////////////////////////////////////////////////////////////////////////////////////////////// 	// 
	
	if( millis() > scheduleDelay || millis() < last_millis_sample )										// To avoid the overflow error (each 49,7 days) 
	{ 
		scheduleDelay = millis() + poolingDelay;
		last_millis_sample = millis();
		
		///////////////////////////////
		// Inicialization - Tx Data
		///////////////////////////////  
		rxTxBuffer[0] = txData[0];
		rxTxBuffer[1] = txData[1];
		rxTxBuffer[2] = txData[2];
		rxTxBuffer[3] = txData[3];
		rxTxBuffer[4] = txData[4];
		rxTxBuffer[5] = txData[5];
		rxTxBuffer[6] = txData[6];
		rxTxBuffer[7]= txData[7];

		rxData[0] = 0;       			                                                               	//
		rxData[1] = 0;       			                                                               	//
		rxData[2] = 0;       			                                                               	//
		rxData[3] = 0;       			                                                               	//
		rxData[4] = 0;       			                                                               	//
		rxData[5] = 0;       			                                                               	//
		rxData[6] = 0;       			                                                               	//
		rxData[7] = 0;       			                                                               	//
		
		txSequenceId++;																					// Packet sequence increment

		fullDataExchange = fullDataExchange + 1; 														// This feature is not supported in this release (estimated on 1000 bytes) (400 bytes is the average Rx+Tx for HTTP (full packet HTTP) retransmissions and overhead => x 2,5

		//--------------------------------------------------------------------							//
			#if DEBUG_DATA																				//
			if( rxRemoteDebug ) {
				Serial.println("-----------------------------------");									//
				Serial.println("STEP A> Values Sent to NearHUB");										//
				Serial.println( deviceName );															//
				Serial.println( deviceSignature );														//
				Serial.println( "Header |seq|ack|cmd|dly|clk|acu|" ); 									//
				Serial.println( txSequenceId );															//
				Serial.println( txSeqAck );																//
				Serial.println( txCommand );  															//
				Serial.println( "0" );
				Serial.println( "0" );
				Serial.println( fullDataExchange );				
				Serial.println( "Register_A |a0|a1|a2|a3|a4|a5|a6|a7|" );          						//
				for( i=0 ; i<4 ; i++ ) {
					sprintf( auxData, HEX_FORMAT, (i*2), rxTxBuffer[i*2] );
					Serial.println( auxData );															//
					if( rxNearMode == 1 ) 
						sprintf( auxData, DEC_FORMAT,(i*2)+1, rxTxBuffer[(i*2)+1] );
					else 
						sprintf( auxData, HEX_FORMAT,(i*2)+1, rxTxBuffer[(i*2)+1] );
					Serial.println( auxData );		
				}
				Serial.println( "" );
			}
			#endif            																			//
		 //--------------------------------------------------------------------	 						// 
		 
			
		/////////////////////////////////////////////////////////////                                   //
		// Sending Data - Call to makePost() (prints HTTP data)	                                     	//
		/////////////////////////////////////////////////////////////                                   //
		if( rxRemoteDebug ) {
			digitalWrite( NEAR_LED, HIGH);		
		}
		MakePost();                                                                              		//
		if( rxRemoteDebug ) {
			digitalWrite( NEAR_LED, LOW );	
		}
		
		/////////////////////////////////////////////////////////////                                   //
		// Data Reception                                                                               //
		/////////////////////////////////////////////////////////////                                   //
		ready = 0;                                                                                      //

		//--------------------------------------------------------------------
		   #if DEBUG_BETA
				Serial.println("DEBUG> Waitting for Server Response");
		   #endif
		//--------------------------------------------------------------------

		for (i=0; i < 20; i++)
		{                                                                       						//
			if( ReadData() == 1 ){
				break;
			}																							//
			if( ready ){                                                                                // Ready=1 => There are new Rx Data 
				break;                                                                                  //
			}                                                                                           //
			delay(500);                                                                                 // Wait up to 10 sec (the Cloud delay response can exceed 7000 ms )
		}                																				//
		
		/////////////////////////////////////////////////////////////
		// Disconnect from NearHub
		/////////////////////////////////////////////////////////////
		client.flush();																				
		client.stop();                           
		
		/////////////////////////////////////////////////////////////
		// Rx Frame Verification
		/////////////////////////////////////////////////////////////
		frameRxError = 0;
		  
		if ( ready == 0 )                                                                              	// TimeOut, there is no data received from the Cloud
		{
			frameRxError = 1;                                                                         	// No Rx data
			 //--------------------------------------------------------------------
			   #if DEBUG_ERROR
					Serial.println("ERROR> No Response from NearHuUB");
			   #endif
			//--------------------------------------------------------------------
		}
		if ( hubDataRxError == 1 )                                                                     	// Data Rx but with error
		{	  
			  frameRxError = 1;                                                                         //
			 //--------------------------------------------------------------------
			   #if DEBUG_ERROR
					Serial.println("ERROR> Corrupted Packet received from NearHUB");
			   #endif
			//--------------------------------------------------------------------
		} 

		//***************************************					                                   	//
		// Reset Tx Packet                                                             					//
		//***************************************					                                   	//	
		rxData[0] = 0;       			                                                               	//
		rxData[1] = 0;       			                                                               	//
		rxData[2] = 0;       			                                                               	//
		rxData[3] = 0;       			                                                               	//
		rxData[4] = 0;       			                                                               	//
		rxData[5] = 0;       			                                                               	//
		rxData[6] = 0;       			                                                               	//
		rxData[7] = 0;       			                                                               	//
		
		txSeqAck  = 1;																					// Default set to 1
		txCommand = (ULONG) rxNearMode;
		
		//***************************************					                                   	//
		// Rx Frame Error                                                                     			//
		//***************************************					                                   	//
		if (frameRxError == 1)
		{																								//
			frameRxError = 0;                                                                           //
			*ret = 50;																					//
			return;   
		}
		else 
		{  
			/////////////////////////////////////////////////////////////                             	//
			// Frame Received OK  - ( sequence verification )
			/////////////////////////////////////////////////////////////                              	// 			 
			
			//***************************************
			// [Error 50] - Authentication Mismatch
			//***************************************
			if( strcmp( rxDeviceName, deviceName ) != 0 ||  strcmp( rxSignature, deviceSignature ) != 0 )
			{
				(*ret) = 50;   			
				//--------------------------------------------------------------------
			       #if DEBUG_ERROR
						Serial.println("ERROR> Packet Authentication Mismatch");
				   #endif
				//--------------------------------------------------------------------
				return;			
			}
			//***************************************
			// [Error 51] - Packet Out of Sequence 
			//***************************************
			if( rxSequenceId != txSequenceId )
			{
				(*ret) = 51;   																			//	
				 //--------------------------------------------------------------------
			       #if DEBUG_ERROR
						Serial.println("ERROR> Packet Out of Sequence");
				   #endif
				 //--------------------------------------------------------------------
				return;
			}
			//***************************************
			// [Error 52] - Packet ACK Error 
			//***************************************
			else if( rxSeqAck != 0 )
			{
				(*ret) = 52;  																			//
				 //--------------------------------------------------------------------
				   #if DEBUG_ERROR
						Serial.println("ERROR> TX Packet - ACK_ERROR");
				   #endif
				 //--------------------------------------------------------------------
				return;
			}
			//***************************************
			// Packet Tx/Rx = OK
			//***************************************
			else
			{	  			
				txSeqAck = 0;																			// Set ACK = OK
			}
			
			if( rxNearMode == 2 )
			{
				//***************************************
				// TRNSP MODE [20]
				//***************************************
				rxData[0] = rxTxBuffer[0];                                                  			//
				rxData[1] = rxTxBuffer[1];                                                  			//             
				rxData[2] = rxTxBuffer[2];                                                  			//
				rxData[3] = rxTxBuffer[3];                                                 				//
				rxData[4] = rxTxBuffer[4];                                                 				//
				rxData[5] = rxTxBuffer[5];                                                 				//
				rxData[6] = rxTxBuffer[6];                                                 				//
				rxData[7] = rxTxBuffer[7];                                                 				//
				(*ret) = 20; 
			}
			else if ( rxNearMode == 1 )			
			{
				//***************************************
				// VMCU MODE [10]
				//***************************************			
				rxData[0] = rxTxBuffer[0];                                                  			//
				rxData[1] = rxTxBuffer[1];                                                  			//             
				rxData[2] = rxTxBuffer[2];                                                  			//
				rxData[3] = rxTxBuffer[3];                                                 				//
				rxData[4] = rxTxBuffer[4];                                                 				//
				rxData[5] = rxTxBuffer[5];                                                 				//
				rxData[6] = rxTxBuffer[6];                                                 				//
				rxData[7] = rxTxBuffer[7];                                                 				//			
				
				txData[0] = 0;                                                                 			//
				txData[1] = 0;                                                                  		//
				txData[2] = 0;                                                                  		//
				txData[3] = 0;                                                                  		//
				txData[4] = 0;                                                                  		//
				txData[5] = 0;                                                                  		//				
				txData[6] = 0;                                                                  		//				
				txData[7] = 0;                                                                  		//

				(*ret) = 10;  																			
			
				/////////////////////////////////////////////////////////////
				// Processing NearBIOS Services
				/////////////////////////////////////////////////////////////
				
				for( i=0; i<CHANNELS_NUMBER ; i++ )
				{
					//***************************************
					// NBIOS Command Processing 
					//***************************************
					vmcuRxMethod  = (byte)( rxData[i*2] >> 24 ); 										// 8 bits (1=> GET, 2=>POST)
					auxService = (UINT)( rxData[i*2] & 0x00FFFFFF ); 									// 16 bits
					retValue = 0;
					
					NearBiosMainSwitch( auxService, rxData[(i*2)+1], &retValue, vmcuRxMethod ); 		// Arg: Service(16b), Value(32b), return(32b)
										
					txData[i*2] = rxData[i*2];															// 32 bits
					txData[(i*2)+1] = retValue;															// 32 bits
				}	
			}
			else
			{	
				//***************************************
				// Unsupported Command [53]
				//***************************************
				(*ret) = 53;
				// Unsupported Command
			}
	
			/////////////////////////////////////////////////////////////
			// Refresh Polling Timer
			/////////////////////////////////////////////////////////////			
			poolingDelay = (ULONG) rxPoolingDelay;						
			

			//--------------------------------------------------------------------						//
			 #if DEBUG_DATA
			if( rxRemoteDebug ) {
				Serial.println("STEP B> Values Received from NearHub"); 								//
				Serial.println( deviceName );															//
				Serial.println( "Header |seq|ack|cmd|dly|clk|acu|" ); 									//
				Serial.println( rxSequenceId );															//
				Serial.println( rxSeqAck );																//
				Serial.println( rxCommand );  															// 	
				Serial.println( rxPoolingDelay ); 														//                      
				Serial.println( rxServerDelay ); 														//  
				Serial.println( rxDataExchange );
				Serial.println( "Register_B |b0|b1|b2|b3|b4|b5|b6|b7|" );          						//
				for( i=0 ; i<4 ; i++ ) {
					sprintf( auxData, HEX_FORMAT, (i*2), rxTxBuffer[i*2] );
					Serial.println( auxData );															//
					if( rxNearMode == 1 ) 
						sprintf( auxData, DEC_FORMAT,(i*2)+1, rxTxBuffer[(i*2)+1] );
					else 
						sprintf( auxData, HEX_FORMAT,(i*2)+1, rxTxBuffer[(i*2)+1] );
					Serial.println( auxData );	
				}	
			}
			#endif      																				//
			//--------------------------------------------------------------------         				//
			
			return;
		}
	}

	*ret = 0; 
	return;	
}


/*####################################################################################################################################
#######################################################################################################################################
###													END OF MAIN CODE																###
#######################################################################################################################################
 ####################################################################################################################################*/

