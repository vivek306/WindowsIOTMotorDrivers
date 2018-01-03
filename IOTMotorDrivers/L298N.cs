using IOTMotorDrivers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace IOTMotorDrivers
{
    public class L298N : IDisposable
    {
        private PCA9685 GetPCA9685 { get; set; }

        public bool IsInitialized { get; set; } = false;
        // GPIO pins
        private GpioPin IN1, IN2, IN3, IN4;
        // PWM pins
        int ENA = -1, ENB = -1;

        public bool Motor1Available = false, Motor2Available = false;

        private void SetGPIOWrite1(int in1, int in2)
        {
            if (in1 == 0 || in1 == -1) IN1.Write(GpioPinValue.Low);
            if (in1 == 1) IN1.Write(GpioPinValue.High);
            if (in1 == -1) IN1.Dispose();

            if (in2 == 0 || in2 == -1) IN2.Write(GpioPinValue.Low);
            if (in2 == 1) IN2.Write(GpioPinValue.High);
            if (in2 == -1) IN2.Dispose();
        }

        private void SetGPIOWrite2(int in3, int in4)
        {
            if (in3 == 0 || in3 == -1) IN3.Write(GpioPinValue.Low);
            if (in3 == 1) IN3.Write(GpioPinValue.High);
            if (in3 == -1) IN3.Dispose();


            if (in4 == 0 || in4 == -1) IN4.Write(GpioPinValue.Low);
            if (in4 == 1) IN4.Write(GpioPinValue.High);
            if (in4 == -1) IN4.Dispose();
        }

        /// <summary>
        /// Create an instance of the L298N driver (PCA9685 driver can be used as extension to have more
        /// stable PWM outputs as Windows IOT is not a real time operating system)
        /// </summary>
        /// <param name="motor1">initialize the motor1 with available pins for the driver</param>
        /// <param name="motor2">initialize the motor2 with available pins for the driver</param>
        /// <param name="pwnDriver">pwm driver to control the speed of the motor (PCA9685 chip)</param>
        public L298N(L298NMotors motor1, L298NMotors motor2, PCA9685 pwnDriver = null)
        {
            var gpio = GpioController.GetDefault();
            if (gpio != null)
            {
                GetPCA9685 = pwnDriver;

                if (GetPCA9685 != null) GetPCA9685.SetDesiredFrequency(60);

                if(motor1 != null)
                {
                    IN1 = gpio.OpenPin(motor1.INAPin);
                    IN2 = gpio.OpenPin(motor1.INBPin);

                    IN1.SetDriveMode(GpioPinDriveMode.Output);
                    IN2.SetDriveMode(GpioPinDriveMode.Output);

                    ENA = motor1.ENPin;

                    SetGPIOWrite1(0, 0);

                    if (GetPCA9685 != null) GetPCA9685.SetPulseParameters(ENA, 0);

                    Motor1Available = true;
                }

                if (motor2 != null)
                {
                    IN3 = gpio.OpenPin(motor2.INAPin);
                    IN4 = gpio.OpenPin(motor2.INBPin);

                    IN3.SetDriveMode(GpioPinDriveMode.Output);
                    IN4.SetDriveMode(GpioPinDriveMode.Output);

                    ENB = motor2.ENPin;

                    SetGPIOWrite2(0, 0);

                    if (GetPCA9685 != null) GetPCA9685.SetPulseParameters(ENB, 0);

                    Motor2Available = true;
                }
                
                if(Motor1Available || Motor2Available) IsInitialized = true;
            }
        }

        /// <summary>
        /// speed in percent for motor 1
        /// </summary>
        public void Motor1Speed(double speedPercent)
        {
            if (Motor1Available)
            {
                if (GetPCA9685 != null) GetPCA9685.SetPulseParameters(ENA, GetSpeed(speedPercent));
            }
        }

        /// <summary>
        /// speed in percent for motor 2
        /// </summary>
        public void Motor2Speed(double speedPercent)
        {
            if (Motor2Available)
            {
                if (GetPCA9685 != null) GetPCA9685.SetPulseParameters(ENB, GetSpeed(speedPercent));
            }
        }

        /// <summary>
        /// direction for motor 1 (Direction resets the Speed)
        /// </summary>
        public void Motor1Direction(bool isClockwise)
        {
            if (Motor1Available)
            {
                if (isClockwise) SetGPIOWrite1(1, 0);
                else SetGPIOWrite1(0, 1);
            }
        }

        /// <summary>
        /// direction for motor 2 (Direction resets the Speed)
        /// </summary>
        public void Motor2Direction(bool isClockwise)
        {
            if (Motor2Available)
            {
                if (isClockwise) SetGPIOWrite2(1, 0);
                else SetGPIOWrite2(0, 1);
            }
        }

        /// <summary>
        /// set the speed and direction for motor 1
        /// </summary>
        public void StartMotor1(double speedPercent, bool isClockwise)
        {
            if (Motor1Available)
            {
                Motor1Speed(speedPercent);
                Motor1Direction(isClockwise);
            }
        }

        /// <summary>
        /// set the speed and direction for motor 2
        /// </summary>
        public void StartMotor2(double speedPercent, bool isClockwise)
        {
            if (Motor2Available)
            {
                Motor2Speed(speedPercent);
                Motor2Direction(isClockwise);
            }
        }

        private int GetSpeed(double speedPercent)
        {
            var speedSafePercent = speedPercent > 1 || speedPercent < 0.1 ? 0.1 : speedPercent;
            return Convert.ToInt32(speedSafePercent * 4096);
        }

        /// <summary>
        /// start the dc motor
        /// </summary>
        /// <param name="isClockwise1">choose the direction between clockwise and counter clockwise</param>
        /// <param name="isClockwise2">choose the direction between clockwise and counter clockwise</param>
        /// <param name="motorSelection">select the motor(s) to start</param>
        /// <param name="speedPercent1">speed in percent for motor 1</param>
        /// <param name="speedPercent2">speed in percent for motor 2</param>
        public void Start(bool isClockwise1 = false, bool isClockwise2 = false,
            L298NMotorSelection motorSelection = L298NMotorSelection.All, 
            double speedPercent1 = 0.1, double speedPercent2 = 0.1)
        {
            if (IsInitialized)
            {
                if (motorSelection == L298NMotorSelection.Motor1) StartMotor1(speedPercent1, isClockwise1);
                else if (motorSelection == L298NMotorSelection.Motor2) StartMotor2(speedPercent2, isClockwise2);
                else Parallel.Invoke(() => StartMotor1(speedPercent1, isClockwise1), 
                    () => StartMotor2(speedPercent2, isClockwise2));
            }
        }

        private void StopMotor1()
        {
            if (Motor1Available)
            {
                SetGPIOWrite1(0, 0);
                if (GetPCA9685 != null) GetPCA9685.SetPulseParameters(ENA, 0);
            }
        }

        private void StopMotor2()
        {
            if (Motor2Available)
            {
                SetGPIOWrite2(0, 0);
                if (GetPCA9685 != null) GetPCA9685.SetPulseParameters(ENB, 0);
            }
        }

        /// <summary>
        /// stop the step motor
        /// </summary>
        /// <param name="l298NMotor">stop the select motor(s) in a safe way</param>
        public void Stop(L298NMotorSelection l298NMotor = L298NMotorSelection.All)
        {
            if (IsInitialized)
            {
                if (l298NMotor == L298NMotorSelection.Motor1) StopMotor1();
                else if (l298NMotor == L298NMotorSelection.Motor2) StopMotor2();
                else Parallel.Invoke(() => StopMotor1(), () => StopMotor2());
            }
        }

        public void Dispose()
        {
            if (IsInitialized)
            {
                Stop();
                if (Motor1Available) SetGPIOWrite1(-1, -1);
                if (Motor2Available) SetGPIOWrite2(-1, -1);
            }
        }
    }
}
