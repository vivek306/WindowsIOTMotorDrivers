using IOTMotorDrivers.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace IOTMotorDrivers
{
    public class ULN2003 : IDisposable
    {
        // GPIO pins
        private GpioPin IN1, IN2, IN3, IN4;
        // Step Sequence
        private List<int[]> Step_Seqs { get; set; }

        public bool IsInitialized { get; set; } = false;

        public bool IsSupported { get; set; } = true;

        private SafeBreak SafeBreakMe;

        /// <summary>
        /// Create an instance of the ULN2003 driver (currently only supports 28BYJ step motor)
        /// </summary>
        public ULN2003(int IN1pin, int IN2pin, int IN3pin, int IN4pin, ULN2003Enums motorType = ULN2003Enums.STEP_28BYJ)
        {
            if (motorType == ULN2003Enums.Other) IsSupported = false;
            else
            {
                var gpio = GpioController.GetDefault();
                if (gpio != null)
                {
                    IN1 = gpio.OpenPin(IN1pin);
                    IN2 = gpio.OpenPin(IN2pin);
                    IN3 = gpio.OpenPin(IN3pin);
                    IN4 = gpio.OpenPin(IN4pin);

                    IN1.SetDriveMode(GpioPinDriveMode.Output);
                    IN2.SetDriveMode(GpioPinDriveMode.Output);
                    IN3.SetDriveMode(GpioPinDriveMode.Output);
                    IN4.SetDriveMode(GpioPinDriveMode.Output);

                    SetGPIOWrite(0, 0, 0, 0);

                    SetStepSequences(motorType);

                    SafeBreakMe = new SafeBreak();
                    SafeBreakMe.PropertyChanged += SafeBreakMe_PropertyChanged;

                    IsInitialized = true;
                }
            }
        }

        private void SafeBreakMe_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var breakMe = sender as SafeBreak;
            if (e.PropertyName == "IsActive" & !breakMe.IsActive & !breakMe.IsDisposed)
            {
                // Only dispose if the Active is false
                if (breakMe.Dispose)
                {
                    SafeBreakMe.IsDisposed = true;
                    SetGPIOWrite(-1, -1, -1, -1);
                }
                else SetGPIOWrite(0, 0, 0, 0);
            }
        }

        private void SetGPIOWrite(int in1, int in2, int in3, int in4)
        {
            if (in1 == 0 || in1 == -1) IN1.Write(GpioPinValue.Low);
            if (in1 == 1) IN1.Write(GpioPinValue.High);
            if (in1 == -1) IN1.Dispose();

            if (in2 == 0 || in2 == -1) IN2.Write(GpioPinValue.Low);
            if (in2 == 1) IN2.Write(GpioPinValue.High);
            if (in2 == -1) IN2.Dispose();

            if (in3 == 0 || in3 == -1) IN3.Write(GpioPinValue.Low);
            if (in3 == 1) IN3.Write(GpioPinValue.High);
            if (in3 == -1) IN3.Dispose();


            if (in4 == 0 || in4 == -1) IN4.Write(GpioPinValue.Low);
            if (in4 == 1) IN4.Write(GpioPinValue.High);
            if (in4 == -1) IN4.Dispose();
        }

        private void SetStepSequences(ULN2003Enums motorType)
        {
            if (motorType == ULN2003Enums.STEP_28BYJ)
            {
                // Each sequence is a step for the step motor 
                Step_Seqs = new List<int[]>()
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
            }
        }

        private async Task StartSequence(int Steps, int Delay, List<int[]> Local_Step_Seqs, IProgress<ULN2003Progress> progress)
        {
            int seqCounter = 0;
            for (int step = 0; step <= Steps; step++)
            {
                if (seqCounter < Local_Step_Seqs.Count)
                {
                    var step_Seq = Local_Step_Seqs[seqCounter];
                    SetGPIOWrite(step_Seq[0], step_Seq[1], step_Seq[2], step_Seq[3]);
                    await Task.Delay(Delay);
                    if (progress != null)
                        progress.Report(new ULN2003Progress { IsActive = true, CurrentStep = step, CurrentSequence = seqCounter, StepsSize = Steps });
                    seqCounter++;
                }

                if (seqCounter == Local_Step_Seqs.Count) seqCounter = 0;

                if (SafeBreakMe.Break)
                {
                    SafeBreakMe.Break = false;
                    break;
                }
            }
        }

        /// <summary>
        /// start the step motor
        /// </summary>
        /// <param name="revolutions">each revolution executes 4096 steps and hence the lowest is 0.00041 for 1 step</param>
        /// <param name="isClockwise">choose the direction between clockwise and counter clockwise</param>
        /// <param name="delay">time required for executing the steps to move the motor</param>
        /// <param name="progress">get real time updates regarding the steps</param>
        public async Task Start(double revolutions, bool isClockwise = false,
            int delay = 1, IProgress<ULN2003Progress> progress = null)
        {
            if (IsSupported & IsInitialized)
            {
                SafeBreakMe.IsActive = true;
                var revsPercent = Convert.ToInt32(revolutions * 4096);
                // 1 revolution = 4096 steps
                var Steps = revsPercent < 1 ? 4096 : revsPercent;
                var Local_Step_Seqs = new List<int[]>(Step_Seqs);
                if (!isClockwise) Local_Step_Seqs.Reverse();
                var Delay = delay > 1000 || delay < 1 ? 1 : delay;

                await StartSequence(Steps, Delay, Local_Step_Seqs, progress);

                if (progress != null)
                    progress.Report(new ULN2003Progress { IsActive = false });
                SafeBreakMe.IsActive = false;
            }
        }

        /// <summary>
        /// stop the step motor
        /// </summary>
        /// <param name="safeStop">stop the step motor in a safe way</param>
        public void Stop(bool safeStop = true)
        {
            if (IsSupported & IsInitialized)
            {
                if (safeStop)
                {
                    if (SafeBreakMe.IsActive) SafeBreakMe.Break = true;
                }
                else SafeBreakMe.IsActive = false;
            }
        }

        public void Dispose()
        {
            SafeBreakMe.Dispose = true;
            SafeBreakMe.Break = true;
            if (!SafeBreakMe.IsActive) SafeBreakMe.IsActive = false;
        }
    }
}
