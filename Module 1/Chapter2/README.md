Learning-IoT-HTTP
=================

Source code for the HTTP chapter of the book [Learning Internet of Things](https://www.packtpub.com/application-development/learning-internet-things).

This chapter covers the basics of the HTTP protocol. It also shows how to use HTTP in the **sensor**, **actuator** and **controller** projects, each running on separate Raspberry Pis or Raspberry Pi 2s.

The source code contains the following projects:

|Project                          | Description|
|:------------------------------- |:---------- |
|**Actuator**                     | The actuator project. Controlls a sequence of digital outputs as well as an alarm output.|
|**Controller**                   | The controller project. Controls the actuator based on input received from the sensor.|
|**Sensor**                       | The sensor project. Measures and logs temperature, light and movement.|
|**SensorN**                      | Shows the evolution of the sensor project. 1<=N<=6.|
|**Clayster.Library.IoT**         | Library handling basic interoperability for the Internet of Things.|
|**Clayster.Library.RaspberryPi** | Library containing classes handling different types of devices connected to the Raspberry Pi or Raspberry Pi 2 interfaced through GPIO.|
|**Clayster**                     | Contains libraries that facilitate data persistence, event logging, communication, localization and scripting.|

Projects are developed in C# and compiled using [Xamarin](http://xamarin.com/). They are executed on Raspberry Pi or Raspberry Pi 2 using [MONO](http://www.mono-project.com/). By modifying the classes in Clayster.Library.RaspberryPi, the code can be made to run on other hardware platforms as well. This library is the only library that contains code specifically written for the Raspberry Pi and Raspberry Pi 2.

Chapters of the book:

| Chapter | Title                         | Source Code |
| -------:|:----------------------------- |:-----------:|
|         | Preface                       | N/A |
| 1       | Preparing our IoT projects    | N/A |
| 2       | The HTTP Protocol             | [Learning-IoT-HTTP](https://github.com/Clayster/Learning-IoT-HTTP) |
| 3       | The UPnP Protocol             | [Learning-IoT-UPnP](https://github.com/Clayster/Learning-IoT-UPnP) |
| 4       | The CoAP Protocol             | [Learning-IoT-CoAP](https://github.com/Clayster/Learning-IoT-CoAP) |
| 5       | The MQTT Protocol             | [Learning-IoT-MQTT](https://github.com/Clayster/Learning-IoT-MQTT) |
| 6       | The XMPP Protocol             | [Learning-IoT-XMPP](https://github.com/Clayster/Learning-IoT-XMPP) |
| 7       | Using an IoT Service Platform | [Learning-IoT-IoTPlatform](https://github.com/Clayster/Learning-IoT-IoTPlatform) |
| 8       | Creating protocol gateways    | [Learning-IoT-Gateway](https://github.com/Clayster/Learning-IoT-Gateway) |
| 9       | Security and Interoperability | N/A |

[Ulf Bonde](http://learninginternetofthings.com/author/ulf-bonde/) has documented his progress with the book in a series of interesting and well-done video-tutorials on <http://learninginternetofthings.com/>. Some posts that might be of interest to people using code in this project include:

* [Introduction to Internet Of Things](http://learninginternetofthings.com/introduction-to-internet-of-things/)
* [Diving into the HTTP protocol](http://learninginternetofthings.com/diving-into-the-http-protocol/)
