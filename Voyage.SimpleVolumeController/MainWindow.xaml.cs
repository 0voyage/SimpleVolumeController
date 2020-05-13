using System;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NAudio.CoreAudioApi;

namespace Voyage.SimpleVolumeController
{
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //Scroll to change the master volume
            VolumeWheel.MouseWheel += new MouseWheelEventHandler(MultiplyVolumeByScrolling);

            //Update volumes every second
            SetupTimer();
        }

        /// <summary>
        /// Timer to call ReadVolumes() every second
        /// </summary>
        private void SetupTimer()
        {
            //Instance
            var timer = new DispatcherTimer(DispatcherPriority.Normal);
            
            //Read current volumes
            timer.Tick += (s, e) => ReadVolumes();
            
            //Every Second
            timer.Interval = TimeSpan.FromSeconds(1.0);

            //Stop timer when closing
            Closing += (s, e) => timer.Stop();

            //Start timer
            timer.Start();
        }

        /// <summary>
        /// Use NAudio to read current volumes (Master and each session)
        /// </summary>
        private void ReadVolumes()
        {
            Dispatcher.Invoke(() =>
            {
                //Instance
                var mmde = new MMDeviceEnumerator();
                MMDevice device = mmde.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                //DataTable
                var dt = new DataTable();
                dt.Columns.Add(new DataColumn("Key"));
                dt.Columns.Add(new DataColumn("Value"));

                //Data - Time
                dt.Rows.Add("Current Time", DateTime.Now.ToString("HH:mm:ss"));

                //Data - Default Audio Device Info
                dt.Rows.Add("Device Name", device.ToString());
                dt.Rows.Add("Device Minimum (dB)", device.AudioEndpointVolume.VolumeRange.MinDecibels.ToString());
                dt.Rows.Add("Device Maximum (dB)", device.AudioEndpointVolume.VolumeRange.MaxDecibels.ToString());

                //Data - Master Volume Info
                dt.Rows.Add("Master Volume (Scalar 0-1)", device.AudioEndpointVolume.MasterVolumeLevelScalar.ToString());
                dt.Rows.Add("Master Volume (Scalar 0-100)", (device.AudioEndpointVolume.MasterVolumeLevelScalar * 100).ToString() + "%");
                dt.Rows.Add("Master Volume (dB)", device.AudioEndpointVolume.MasterVolumeLevel.ToString());
                dt.Rows.Add("Master Volume Peak (dB)", device.AudioMeterInformation.MasterPeakValue.ToString());

                //Data - Session Volume Info
                for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
                {
                    //Session - ProcessID
                    AudioSessionControl session = device.AudioSessionManager.Sessions[i];
                    uint processID = session.GetProcessID;
                    
                    //Session - App Name
                    String processName = Process.GetProcessById((int)processID).ProcessName;
                    if (session.IsSystemSoundsSession)
                    {
                        processName = "System";
                    }

                    //Add to the datatable
                    String prefix = "Session" + (i + 1) + " ";
                    dt.Rows.Add("----------", "----------");
                    dt.Rows.Add(prefix + "Name (Process ID)", processName + " (" + processID.ToString() + ")");
                    dt.Rows.Add(prefix + "Volume (Scalar)", session.SimpleAudioVolume.Volume.ToString());
                    dt.Rows.Add(prefix + "Master Peak", session.AudioMeterInformation.MasterPeakValue.ToString());

                }

                //Show the table
                volumeData.DataContext = dt.DefaultView;

            });
        }

        /// <summary>
        /// Read the scroll direction and change the master volume
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MultiplyVolumeByScrolling(object sender, MouseWheelEventArgs e)
        {
            //Multiplication Factor
            float factor = 2f;

            //Interface
            var mmde = new MMDeviceEnumerator();
            MMDevice device = mmde.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            //Current Volume (0-1)
            float currentVolumeScalar = device.AudioEndpointVolume.MasterVolumeLevelScalar;

            //New Volume (0-1)
            float newVolumeScalar = currentVolumeScalar;

            //When scrolled
            if (e.Delta > 0)
            {
                //Scroll up to double the volume. The volume is limited at 0.3(30%) to avoid unexpected sound bursts.
                newVolumeScalar = Math.Min(0.3f, currentVolumeScalar * factor);
            }
            else if (e.Delta < 0)
            {
                //Scroll down to halve the volume. The volume can get very close to 0.
                newVolumeScalar = Math.Max(0, currentVolumeScalar / factor);
            }

            //Update
            device.AudioEndpointVolume.MasterVolumeLevelScalar = newVolumeScalar;
            ReadVolumes();
        }

    }
}
