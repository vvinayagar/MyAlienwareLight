using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using LightFX;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Un4seen.Bass;



namespace MyAlienware
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Threading.Timer tmrBar;
        MMDeviceEnumerator enumerator;
        MMDeviceCollection devices;
        BackgroundWorker worker;
        WaveInEvent waveIn;
        Un4seen.Bass.Misc.BPMCounter bPMCounter;
        int _stream = 0;

        public MainWindow()
        {
            InitializeComponent();

            ddLights.Items.Add("All");

            for (int i = 0; i < 11; i++)
            {
                ddLights.Items.Add(i.ToString());
            }

            tmrBar = new System.Threading.Timer(Callback, null, 1, Timeout.Infinite);
            InitWasapi();
        }

        private void TmrBar_Elapsed(object sender, ElapsedEventArgs e)
        {
            Thread thread = new Thread(Callback);
            thread.Start();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Set Audio detection
            enumerator = new MMDeviceEnumerator();
            devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            while (true)
            {
                // Thread.Sleep(500);
                UpdateUI();
            }
        }
        private void Callback(object state)
        {
            enumerator = new MMDeviceEnumerator();
            devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            bPMCounter = new Un4seen.Bass.Misc.BPMCounter(20, 44100);

            lightFX = new LightFXController();
            result = lightFX.LFX_Initialize();
            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 1;

            WaveOutEvent waveOut = new WaveOutEvent();
            while (true)
            {
                Thread.Sleep(50);
                UpdateUI();
            }
            //}
            lightFX.LFX_Release();
            tmrBar.Dispose();
            tmrBar = new System.Threading.Timer(Callback, null, 1, Timeout.Infinite);
        }

        public void UpdateUI()
        {
            StringBuilder sb = new StringBuilder();
            try
            {

                foreach (MMDevice dev in devices)
                {
                    double val = dev.AudioMeterInformation.MasterPeakValue;
                    if (val != 0 && !dev.FriendlyName.Contains("Microphone"))
                    {
                        ColorTrigger(dev.AudioMeterInformation.MasterPeakValue * 100);
                    }

                    try
                    {
                        sb.Append(dev.DeviceFriendlyName + " : " + dev.AudioMeterInformation.PeakValues[0].ToString() + "\n");
                        sb.Append(dev.DeviceFriendlyName + " : " + dev.AudioMeterInformation.PeakValues[1].ToString() + "\n");
                        sb.Append(dev.DeviceFriendlyName + " : " + dev.AudioMeterInformation.MasterPeakValue.ToString() + "\n");
                        sb.Append(dev.FriendlyName + " : " + dev.AudioMeterInformation.MasterPeakValue.ToString() + "\n");
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke((() =>
                        {
                            txtConsole.Text = ex.ToString();
                        }
                        ));
                    }
                }

                Dispatcher.Invoke((() =>
                {

                    txtConsole.Text = sb.ToString();
                }
                      ));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke((() =>
              {
                  //txtConsole.Text = ex.ToString();
              }
              ));
            }
        }

        private void InitWasapi()
        {
            Un4seen.BassWasapi.WASAPIPROC _process = new Un4seen.BassWasapi.WASAPIPROC(Process); // Delegate
            bool res = Un4seen.BassWasapi.BassWasapi.BASS_WASAPI_Init(0, 0, 0, Un4seen.BassWasapi.BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0f, _process, IntPtr.Zero);
            if (!res)
            {
                // Do error checking
            }

            // This is the part you are looking for (maybe!)
            // Use these flags because Wasapi needs 32-bit sample data
            var info = Un4seen.BassWasapi.BassWasapi.BASS_WASAPI_GetInfo();
            _stream = Bass.BASS_StreamCreatePush(info.freq, info.chans, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, IntPtr.Zero);

            Un4seen.BassWasapi.BassWasapi.BASS_WASAPI_Start();
        }

        private int Process(IntPtr buffer, int length, IntPtr user)
        {
            Bass.BASS_StreamPutData(_stream, buffer, length);
            return length;
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {

        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            var lightFX = new LightFXController();

            var result = lightFX.LFX_Initialize();
            if (result == LFX_Result.LFX_Success)
            {
                lightFX.LFX_Reset();

                uint numDevs;
                lightFX.LFX_GetNumDevices(out numDevs);

                txtConsole.Text = "";

                for (uint devIndex = 0; devIndex < numDevs; devIndex++)
                {
                    uint numLights;
                    lightFX.LFX_GetNumLights(devIndex, out numLights);

                    var green = new LFX_ColorStruct(255, Convert.ToByte(txtRed.Text), Convert.ToByte(txtGreen.Text), 0);
                    var red = new LFX_ColorStruct(255, Convert.ToByte(txtRed.Text), Convert.ToByte(txtGreen.Text), 0);
                    for (uint lightIndex = 0; lightIndex < numLights; lightIndex++)
                    {
                        if (ddLights.SelectedIndex == 0)
                        {
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, lightIndex % 2 == 0 ? red : green);
                            //txtConsole.Text += "\nDev Index :" + devIndex.ToString() + "; LightIndex :" + lightIndex.ToString();
                        }
                        else if (Convert.ToInt32(ddLights.SelectedValue) == lightIndex)
                        {
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, lightIndex % 2 == 0 ? red : green);
                            //txtConsole.Text += "\nDev Index :" + devIndex.ToString() + "; LightIndex :" + lightIndex.ToString();
                        }

                    }
                }


                for (uint devIndex = 0; devIndex < numDevs; devIndex++)
                {
                    StringBuilder devDescription;
                    LFX_DeviceType type;

                    result = lightFX.LFX_GetDeviceDescription(devIndex, out devDescription, 255, out type);
                    if (result != LFX_Result.LFX_Success)
                        continue;

                    Console.WriteLine(string.Format("Device: {0} \tDescription: {1} \tType: {2}", devIndex, devDescription, type));

                    uint numLights;
                    lightFX.LFX_GetNumLights(devIndex, out numLights);
                    for (uint lightIndex = 0; lightIndex < numLights; lightIndex++)
                    {
                        StringBuilder description;
                        result = lightFX.LFX_GetLightDescription(devIndex, lightIndex, out description, 255);
                        if (result != LFX_Result.LFX_Success)
                            continue;

                        LFX_ColorStruct color;
                        result = lightFX.LFX_GetLightColor(devIndex, lightIndex, out color);
                        if (result != LFX_Result.LFX_Success)
                            continue;

                        //       Console.WriteLine(string.Format("\tLight: {0} \tDescription: {1} \tColor: {2}", lightIndex, description, color));
                    }
                }

                lightFX.LFX_Update();
                // Console.WriteLine("Done.\r\rPress ENTER key to finish ...");
                // Console.ReadLine();
                lightFX.LFX_Release();
            }
            else
            {
                switch (result)
                {
                    case LFX_Result.LFX_Error_NoDevs:
                        // Console.WriteLine("There is not AlienFX device available.");
                        break;
                    default:
                        // Console.WriteLine("There was an error initializing the AlienFX device.");
                        break;
                }
            }
        }


        LightFXController lightFX;
        LFX_Result result;

        private void ColorTrigger(double val)
        {

            if (result == LFX_Result.LFX_Success)
            {
                lightFX.LFX_Reset();

                uint numDevs;
                lightFX.LFX_GetNumDevices(out numDevs);

                //  txtConsole.Text = "";

                for (uint devIndex = 0; devIndex < numDevs; devIndex++)
                {
                    uint numLights;
                    lightFX.LFX_GetNumLights(devIndex, out numLights);

                    var green = new LFX_ColorStruct(255, 255, 255, 0);
                    var red = new LFX_ColorStruct(255, 255, 0, 0);
                    var blue = new LFX_ColorStruct(255, 0, 255, 255);
                    var yellow = new LFX_ColorStruct(255, 0, 0, 255);
                    var alter = new LFX_ColorStruct(255, 255, 0, 255);
                    var alter2 = new LFX_ColorStruct(255, 0, 255, 0);


                    for (uint lightIndex = 0; lightIndex < numLights; lightIndex++)
                    {
                        if (val < 5)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, yellow);
                        else if (val > 5 && val < 10)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, blue);
                        else if (val > 10 && val < 15)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, green);
                        else if (val > 15 && val < 20)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, red);
                        else if (val > 20 && val < 25)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, alter2);
                        else if (val > 25 && val < 30)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, alter);
                        else if (val > 30 && val < 35)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, yellow);
                        else if (val > 35 && val < 40)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, blue);
                        else if (val > 40 && val < 45)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, green);
                        else if (val > 50)
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, red);
                        else
                        {
                            lightFX.LFX_SetLightColor(devIndex, lightIndex, alter2);
                        }
                        //if (ddLights.SelectedIndex == 0)
                        //{
                        //    lightFX.LFX_SetLightColor(devIndex, lightIndex, lightIndex % 2 == 0 ? red : green);
                        //    //txtConsole.Text += "\nDev Index :" + devIndex.ToString() + "; LightIndex :" + lightIndex.ToString();
                        //}
                        //else if (Convert.ToInt32(ddLights.SelectedValue) == lightIndex)
                        //{
                        //    lightFX.LFX_SetLightColor(devIndex, lightIndex, lightIndex % 2 == 0 ? red : green);
                        //    //txtConsole.Text += "\nDev Index :" + devIndex.ToString() + "; LightIndex :" + lightIndex.ToString();
                        //}

                    }
                }


                for (uint devIndex = 0; devIndex < numDevs; devIndex++)
                {
                    StringBuilder devDescription;
                    LFX_DeviceType type;

                    result = lightFX.LFX_GetDeviceDescription(devIndex, out devDescription, 255, out type);
                    if (result != LFX_Result.LFX_Success)
                        continue;

                    Console.WriteLine(string.Format("Device: {0} \tDescription: {1} \tType: {2}", devIndex, devDescription, type));

                    uint numLights;
                    lightFX.LFX_GetNumLights(devIndex, out numLights);
                    for (uint lightIndex = 0; lightIndex < numLights; lightIndex++)
                    {
                        StringBuilder description;
                        result = lightFX.LFX_GetLightDescription(devIndex, lightIndex, out description, 255);
                        if (result != LFX_Result.LFX_Success)
                            continue;

                        LFX_ColorStruct color;
                        result = lightFX.LFX_GetLightColor(devIndex, lightIndex, out color);
                        if (result != LFX_Result.LFX_Success)
                            continue;

                        //       Console.WriteLine(string.Format("\tLight: {0} \tDescription: {1} \tColor: {2}", lightIndex, description, color));
                    }
                }

                lightFX.LFX_Update();
                // Console.WriteLine("Done.\r\rPress ENTER key to finish ...");
                // Console.ReadLine();
                //lightFX.LFX_Release();
            }
            else
            {
                switch (result)
                {
                    case LFX_Result.LFX_Error_NoDevs:
                        // Console.WriteLine("There is not AlienFX device available.");
                        break;
                    default:
                        // Console.WriteLine("There was an error initializing the AlienFX device.");
                        break;
                }
            }

        }
    }
}
