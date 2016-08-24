import tweepy
import time
import serial
import struct

auth = tweepy.OAuthHandler('SZ3jdFtq6Y25TtgAPJaL9w4wm', 'jQ9MBuy7SL6wgRK1JF9VG6GAOMGGGGIAFevITkNEAMglUNebgK')
auth.set_access_token('3300242354-sJB78WNygLJeNSXdmN7LGxkTKWBck6vYIL79jjE', 'ZGfOgnPBhUD10Odb7DhvWlMt3KsxKxwqlcAbc0HEk21RH')

api = tweepy.API(auth)
ser = serial.Serial('COM3', 9600, timeout=1)
last_tweet="#switchoff"

while True:  # This constructs an infinite loop
	public_tweets = api.user_timeline(screen_name='@pradeeka7',count=1)
	for tweet in public_tweets:
		if '#switchon' in tweet.text: #check if the tweet contains the text #switchon
			print (tweet.text)	#print the tweet
			if last_tweet == "#switchoff":
				if not ser.isOpen(): #if serial port is not open
					ser.flush();	#open the serial port
					ser.write('1') # write 1 on serial port
				print('Write 1 on serial port')	#print message on console
				last_tweet="#switchon"
		elif "#switchoff" in tweet.text:  #check if the tweet contains the text #switchoff
			print (tweet.text)	#print the tweet
			if last_tweet == "#switchon":
				if not ser.isOpen(): #if serial port is not open
					ser.open();	#open the serial port
					ser.open();	#open the serial port
					ser.write("0") # write 0 on serial port
				print('Write 0 on serial port')	#print message on console
				last_tweet="#switchoff"
		else:	
			ser.close()	#close the serial port
			print('invalid tweet')	
		time.sleep(30)	#wait for 30 seconds