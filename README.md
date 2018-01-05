# WindowsIOTMotorDrivers

Windows IOT motor drivers and demo for PCA9695, ULN2003 and L298N chipsets controlling Servo, Stepper and DC motors respectively.

More info on this project can be found in
https://www.hackster.io/vivek306/windows-iot-stepper-servo-and-dc-motors-66c0c8

```c#
// DC Motor Pins 
private const int PCA9685_DC1_Pin = 15, DCInputAPin = 17, DCInputBPin = 27; 
private const int PCA9685_DC2_Pin = 14, DCInputCPin = 23, DCInputDPin = 24; 
// Driver for PCA9685 
pwmDriver = new PCA9685(); 
pwmDriver.SetDesiredFrequency(60); 
// Driver for L298N 
dcMotorDriver = new L298N(new L298NMotors 
{ 
    ENPin = PCA9685_DC1_Pin, 
    INAPin = DCInputAPin, 
    INBPin = DCInputBPin 
}, new L298NMotors 
{ 
    ENPin = PCA9685_DC2_Pin, 
    INAPin = DCInputCPin, 
    INBPin = DCInputDPin 
}, pwmDriver); 
// Start and control Motor 1
dcMotorDriver.Start(motorSelection: L298NMotorSelection.Motor1, speedPercent1: 0.2, isClockwise1: true); 
// Start both Motor 1 and 2 parallely
// dcMotorDriver.Start(motorSelection: L298NMotorSelection.All, speedPercent1: 0.2, isClockwise1: true, speedPercent2: 0.2, isClockwise2: true);
// To stop the Stepper Motor
stepMotorDriver.Stop();
```
