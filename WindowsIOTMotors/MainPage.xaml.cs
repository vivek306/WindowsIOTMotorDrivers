using IOTMotorDrivers;
using IOTMotorDrivers.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowsIOTMotors
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PCA9685 pwmDriver;
        private L298N dcMotorDriver;
        private ULN2003 stepMotorDriver;

        // Servo Motor Pins
        private const int PCA9685_Servo_Pin = 0;
        // DC Motor Pins
        private const int PCA9685_DC1_Pin = 15, DCInputAPin = 17, DCInputBPin = 27;
        private const int PCA9685_DC2_Pin = 14, DCInputCPin = 23, DCInputDPin = 24;
        // Step Motor Pins
        private const int Step_IN1 = 6, Step_IN2 = 13, Step_IN3 = 19, Step_IN4 = 26;

        // Servo Max and Min Range
        private const int ServoMaxPWM = 450;
        private const int ServoMinPWM = 150;

        // Stopwatch
        private Stopwatch stopwatch;

        // Stepper LED Simulation Sequence
        List<int[]> Step_Seqs = new List<int[]>()
                {
                    new int[] { 1, 0, 0, 0 },
                    new int[] { 1, 1, 0, 0 },
                    new int[] { 0, 1, 0, 0 },
                    new int[] { 0, 1, 1, 0 },
                    new int[] { 0, 0, 1, 0 },
                    new int[] { 0, 0, 1, 1 },
                    new int[] { 0, 0, 0, 1 },
                    new int[] { 1, 0, 0, 1 }
                };

        public MainPage()
        {
            this.InitializeComponent();

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
            // Driver for ULN2003
            stepMotorDriver = new ULN2003(Step_IN1, Step_IN2, Step_IN3, Step_IN4);

            stopwatch = new Stopwatch();

            this.Unloaded += MainPage_Unloaded;

            // Buggy control 
            StepsProgressGuage.HighFontSize = 30;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            stepMotorDriver.Dispose();
            dcMotorDriver.Dispose();
            pwmDriver.Dispose();
        }

        #region Servo Motor

        private void ServoGrid_Loaded(object sender, RoutedEventArgs e)
        {
            ServoSlider.Minimum = ServoMinPWM;
            ServoSlider.Maximum = ServoMaxPWM;
            ServoSlider.Value = (ServoMaxPWM + ServoMinPWM) / 2;

            ServoGuage.From = ServoSlider.Minimum;
            ServoGuage.To = ServoSlider.Maximum;
        }

        private void ServoSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (pwmDriver != null)
            {
                int defaultVal = (ServoMaxPWM + ServoMinPWM) / 2;
                Int32.TryParse(ServoSlider.Value.ToString(), out defaultVal);
                pwmDriver.SetPulseParameters(0, defaultVal, false);
                ServoGuage.Value = ServoSlider.Value;
            }
        }

        #endregion

        #region DC Motors

        private void DCGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (dcMotorDriver.Motor1Available) DCMotorSlider1.Header = "Motor 1 - Activated";
            if (dcMotorDriver.Motor2Available) DCMotorSlider2.Header = "Motor 2 - Activated";
        }

        #region DC Motor 1
        private void DC1StartButton_Click(object sender, RoutedEventArgs e)
        {
            dcMotorDriver.Start(motorSelection: L298NMotorSelection.Motor1, speedPercent1: DCMotorSlider1.Value / 100, isClockwise1: Motor1Direction.IsOn);
        }

        private void DC1StopButton_Click(object sender, RoutedEventArgs e)
        {
            dcMotorDriver.Stop(L298NMotorSelection.Motor1);
        }

        private void DCMotorSlider1_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Motor1Speed.Value = DCMotorSlider1.Value;
            dcMotorDriver.StartMotor1(Motor1Speed.Value / 100, Motor1Direction.IsOn);
        }

        private void Motor1Direction_Toggled(object sender, RoutedEventArgs e)
        {
            dcMotorDriver.StartMotor1(Motor1Speed.Value / 100, Motor1Direction.IsOn);
        }

        #endregion

        #region DC Motor 2
        private void DC2StartButton_Click(object sender, RoutedEventArgs e)
        {
            dcMotorDriver.Start(motorSelection: L298NMotorSelection.Motor2, speedPercent2: DCMotorSlider2.Value / 100, isClockwise2: Motor2Direction.IsOn);
        }

        private void DC2StopButton_Click(object sender, RoutedEventArgs e)
        {
            dcMotorDriver.Stop(L298NMotorSelection.Motor2);
        }

        private void DCMotorSlider2_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Motor2Speed.Value = DCMotorSlider2.Value;
            dcMotorDriver.StartMotor2(Motor2Speed.Value / 100, Motor2Direction.IsOn);
        }

        private void Motor2Direction_Toggled(object sender, RoutedEventArgs e)
        {
            dcMotorDriver.StartMotor2(Motor2Speed.Value / 100, Motor2Direction.IsOn);
        }
        #endregion

        #endregion

        #region Stepper Motor

        private void StepperGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (stepMotorDriver.IsInitialized & stepMotorDriver.IsSupported) StepperMotorSlider.Header = "Stepper Motor Revolutions - Activated";
        }

        private void SetLedSimulation(int in1, int in2, int in3, int in4)
        {
            if (in1 == 0) StepLed1.Foreground = new SolidColorBrush(Colors.Black);
            else StepLed1.Foreground = new SolidColorBrush(Colors.Maroon);

            if (in2 == 0) StepLed2.Foreground = new SolidColorBrush(Colors.Black);
            else StepLed2.Foreground = new SolidColorBrush(Colors.Maroon);

            if (in3 == 0) StepLed3.Foreground = new SolidColorBrush(Colors.Black);
            else StepLed3.Foreground = new SolidColorBrush(Colors.Maroon);

            if (in4 == 0) StepLed4.Foreground = new SolidColorBrush(Colors.Black);
            else StepLed4.Foreground = new SolidColorBrush(Colors.Maroon);
        }

        private async void StepperStartButton_Click(object sender, RoutedEventArgs e)
        {
            double defaultVal = 0;
            Double.TryParse(StepperMotorSlider.Value.ToString(), out defaultVal);

            stopwatch.Reset();
            var progress = new Progress<ULN2003Progress>();
            progress.ProgressChanged += (s, arg) =>
            {
                if (arg.IsActive)
                {
                    StepsProgressGuage.From = 0;
                    StepsProgressGuage.To = arg.StepsSize;
                    StepsProgressGuage.Value = arg.CurrentStep;
                    var seq = Step_Seqs[arg.CurrentSequence];
                    SetLedSimulation(seq[0], seq[1], seq[2], seq[3]);
                }
                else
                {
                    StepsProgressGuage.Value = 0;
                    SetLedSimulation(0, 0, 0, 0);
                }
            };



            stopwatch.Start();
            await stepMotorDriver.Start(revolutions: defaultVal, isClockwise: StepperMotorDirection.IsOn, progress: progress);
            stopwatch.Stop();
            StepperTimeTaken.Text = Math.Round(defaultVal, 2) + " revolution(s) in " + Math.Round(stopwatch.Elapsed.TotalMinutes, 2) + " min(s) ";
        }

        private void StepperStopButton_Click(object sender, RoutedEventArgs e)
        {
            stepMotorDriver.Stop();
            StepperTimeTaken.Text = "Motor was stopped";
        }

        #endregion
    }
}
