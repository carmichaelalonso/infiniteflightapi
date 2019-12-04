using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace InfiniteFlight.ApiClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private State gearState;

        InfiniteFlightAPIClient client = new InfiniteFlightAPIClient();

        public MainWindow()
        {
            InitializeComponent();

            client.ManifestReceived += Client_ManifestReceived;
            client.StateReceived += Client_StateReceived;

            client.Connect();
            //client.Connect("192.168.86.33");


            System.Timers.Timer t = new System.Timers.Timer();            
            t.Elapsed += T_Elapsed;
            t.Interval = 3000;
            t.Start();

        }

        private void Client_StateReceived(object sender, EventArgs e)
        {
            int stateID = (int)sender;

            var state = client.StateByID[stateID];

            if (stateID == flapsStopsID)
            {
                Console.WriteLine("Received flaps stops");

                flapsStops = (int)state.Value; // we know it's an int
            }
        }

        int flapsStopsID = -1;
        int flapsStops = -1;

        private void Client_ManifestReceived(object sender, EventArgs e)
        {
            ////var regex = new Regex("configuration/flaps/\\d+/name");
            ////var match = regex.Match("configuration/flaps/2/name");
            ////foreach (var item in match)
            ////{

            ////}

            //if (Regex.IsMatch("configuration/flaps/0/name", "configuration/flaps/\\w/name"))
            //{

            //}

            //Dispatcher.BeginInvoke((Action)(() =>
            //{
            //    var info = client.StateInfo.Where(x => Regex.IsMatch(x.Path, "configuration/flaps/\\w/name"));

            //    List<int> flapsStops = new List<int>();

            //    for (int i = 0; i < info.Count(); i++)
            //    {

            //    }
                                
            //    flapsComboBox.ItemsSource = flapsStops;
            //});

            //flapsStopsID = info.ID;

            //client.GetState(info.ID);
        }

        double altitude = 0;
        double heading = 0;
        double pitch = 0;
        double roll = 0;

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            heading += 1;

           //client.UpdateAirplanePosition(37.458125, -122.112073, altitude, heading, pitch, roll);

           //client.RefreshAllValues();

            Dispatcher.BeginInvoke((Action)(() =>
            {
                lock (client.States)
                {
                    var builder = new StringBuilder();

                    foreach (var state in client.States)
                    {
                        builder.AppendFormat("{0}: {1}\n", state.Path.PadRight(100, '.'), state.Value);
                    }

                    stateTextBlock.Text = builder.ToString();


                    // get the flaps states now
                    if (flapsComboBox.ItemsSource == null)
                    {
                        var flapsNames = client.States.Where(x => Regex.IsMatch(x.Path, "configuration/flaps/\\w/name")).ToArray();
                        var flapsAngles = client.States.Where(x => Regex.IsMatch(x.Path, "configuration/flaps/\\w/angle")).ToArray();

                        List<string> flapsStateList = new List<string>();

                        for (int i = 0; i < flapsNames.Length; i++)
                        {
                            flapsStateList.Add(string.Format("{0} ({1} deg)", flapsNames[i].Value.ToString(), flapsAngles[i].Value.ToString()));
                        }

                        flapsComboBox.ItemsSource = flapsStateList;
                        //if (Regex.IsMatch("configuration/flaps/0/name", "configuration/flaps/\\w/name"))
                        //{
                    }

                    // get the gear state
                    gearState = client.States.Where(x => x.Path == "aircraft/0/systems/landing_gear/state").FirstOrDefault();
                    if (gearState != null)
                        gearButton.Content = string.Format("Gear {0}", gearState.Value);


                    // get the cameras
                    if (camerasComboBox.ItemsSource == null)
                    {
                        var cameraNames = client.States.Where(x => Regex.IsMatch(x.Path, "infiniteflight/cameras/\\w/name")).ToArray();

                        List<string> cameraList = new List<string>();

                        for (int i = 0; i < cameraNames.Length; i++)
                        {
                            cameraList.Add(cameraNames[i].Value.ToString());
                        }

                        camerasComboBox.ItemsSource = cameraList;

                        var currentState = client.States.Where(x => x.Path == "infiniteflight/current_camera").FirstOrDefault();

                        if (currentState != null)
                            camerasComboBox.SelectedValue = currentState.Value;

                    }

                }

                if (commandsStackPanel.Children.Count == 0)
                {
                    foreach (var command in client.Commands)
                    {
                        var button = new System.Windows.Controls.Button
                        {
                            Content = command.Path,
                            Tag = command,
                            Margin = new Thickness(5),
                            Height = 35
                        };

                        button.Click += Button_Click;

                        commandsStackPanel.Children.Add(button);
                    }
                }
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var command = (sender as Button).Tag as CommandInfo;

            client.RunCommand(command.ID);
        }

        int G5Page = 0;

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var info = client.StateInfo.Where(x => x.Path == "aircraft/instruments/garmin_g5/0/page").FirstOrDefault();

            if (G5Page == 0)
                G5Page = 1;
            else
                G5Page = 0;

            client.SetState(info.ID, G5Page);

            toggleButton.Content = string.Format("Page: {0}", G5Page);
        }

        private void AxisSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var info = client.StateInfo.Where(x => x.Path == "api_joystick/axes/0/value").FirstOrDefault();
            
            client.SetState(info.ID, (int)e.NewValue);
        }

        private void AddAircraft_Click(object sender, RoutedEventArgs e)
        {
            client.AddAircraft("ef677903-f8d3-414f-a190-233b2b855d46",
                "e41c8c2a-233e-494b-a44b-7adc9c91b6b3", 37.458125, -122.112073);
        }

        private void GearButton_Click(object sender, RoutedEventArgs e)
        {
            var newValue = (bool)gearState.Value;
            client.SetState(gearState.ID, !newValue);
            client.GetState(gearState.ID); // trigger a refresh of the UI
        }

        private void FlapsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var info = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/flaps/state").FirstOrDefault();

            var index = flapsComboBox.SelectedIndex;

            client.SetState(info.ID, index);
        }

        private void CamerasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var info = client.StateInfo.Where(x => x.Path == "infiniteflight/current_camera").FirstOrDefault();

            var name = (string)camerasComboBox.SelectedValue;

            if (!string.IsNullOrEmpty(name))
                client.SetState(info.ID, name);
        }

        private void CameraSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var camera = client.StateInfo.Where(x => x.Path == "infiniteflight/cameras/1/y_angle").FirstOrDefault();
            client.SetState(camera.ID, cameraSlider.Value);
            var overrideState = client.StateInfo.Where(x => x.Path == "infiniteflight/cameras/1/angle_override").FirstOrDefault();
            client.SetState(overrideState.ID, true);
        }

        private void MixtureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var info = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/engines/0/mixture_lever").FirstOrDefault();
            client.SetState(info.ID, (float)mixtureSlider.Value);
        }

        private void StartupButton_Click(object sender, RoutedEventArgs e)
        {
            // Flaps down
            FlapsDown(client.StateInfo.Where(x => x.Path == "aircraft/0/systems/flaps/state").FirstOrDefault());

            var beacon = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/beacon_lights_switch/state").FirstOrDefault().ID;
            var nav = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/nav_lights_switch/state").FirstOrDefault().ID;
            client.SetState(beacon, 1);
            client.SetState(nav, 1);

            Thread.Sleep(10000);

            // Mixture rich (100% - push it in steadily but slowly)
            IncrementMixture(client.StateInfo.Where(x => x.Path == "aircraft/0/systems/engines/0/mixture_lever").FirstOrDefault());

            Thread.Sleep(1000);

            // Master and alt on red buttons at the same time if possible)
            var master = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/master_switch/state").FirstOrDefault().ID;
            var alternator = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/alternator_switch/state").FirstOrDefault().ID;
            client.SetState(master, 1);
            client.SetState(alternator, 1);

            Thread.Sleep(1000);

            // Fuel pump on
            var fuelPump = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/engines/0/fuel_pump/state").FirstOrDefault().ID;
            client.SetState(fuelPump, 1);

            Thread.Sleep(1000);

            // Advance throttle to 100% for 2 seconds, then back to idle for 2 seconds
            IncrementThrottleSlider(client.StateInfo.Where(x => x.Path == "simulator/throttle").FirstOrDefault());
            Thread.Sleep(2000);
            DecrementThrottleSlider(client.StateInfo.Where(x => x.Path == "simulator/throttle").FirstOrDefault());

            Thread.Sleep(2000);

            // Start engine
            client.SetState(client.StateInfo.Where(x => x.Path == "aircraft/0/systems/engines/0/starter/state").FirstOrDefault().ID, 4);

            Thread.Sleep(6000);

            // Throttle to 1000rpm
            IncrementThrottleSlider(client.StateInfo.Where(x => x.Path == "simulator/throttle").FirstOrDefault(), -590);

            Thread.Sleep(2000);

            // Avionics master on
            var avionics = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/avionics_bus_1_switch/state").FirstOrDefault().ID;
            client.SetState(avionics, 1);

            Thread.Sleep(1000);

            // Flaps up in stages
            FlapsUp(client.StateInfo.Where(x => x.Path == "aircraft/0/systems/flaps/state").FirstOrDefault());

            Thread.Sleep(1000);

            // Lean mixture to 90%ish
            LeanMixture(client.StateInfo.Where(x => x.Path == "aircraft/0/systems/engines/0/mixture_lever").FirstOrDefault());
        }

        private void SecondJasonButton_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(80000);

            // Avionics master on
            var avionics = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/avionics_bus_1_switch/state").FirstOrDefault().ID;
            client.SetState(avionics, 0);

            Thread.Sleep(2000);

            // Lean mixture to 90%ish
            LeanMixture(client.StateInfo.Where(x => x.Path == "aircraft/0/systems/engines/0/mixture_lever").FirstOrDefault());

            Thread.Sleep(2000);

            // Master and alt on red buttons at the same time if possible)
            var master = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/master_switch/state").FirstOrDefault().ID;
            var alternator = client.StateInfo.Where(x => x.Path == "aircraft/0/systems/electrical_switch/alternator_switch/state").FirstOrDefault().ID;
            client.SetState(master, 0);
            client.SetState(alternator, 0);

            Thread.Sleep(2000);

            client.SetState(client.StateInfo.Where(x => x.Path == "aircraft/0/systems/engines/0/starter/state").FirstOrDefault().ID, 0);
        }

        void IncrementMixture(StateInfo info)
        {
            for (float i = 0.0f; i < 1.05f; i += 0.05f)
            {
                client.SetState(info.ID, i);
                Thread.Sleep(40);
            }
        }
        void LeanMixture(StateInfo info)
        {
            for (float i = 1.0f; i > -0.05f; i -= 0.01f)
            {
                client.SetState(info.ID, i);
                Thread.Sleep(40);
            }
        }

        void IncrementThrottleSlider(StateInfo info, int max = 1000)
        {
            for (int i = -1000; i <= max; i += 100)
            {
                client.SetState(info.ID, i);
                Thread.Sleep(40);
            }
        }

        void DecrementThrottleSlider(StateInfo info)
        {
            for (int i = 1000; i >= -1000; i -= 100)
            {
                client.SetState(info.ID, i);
                Thread.Sleep(40);
            }
        }

        void FlapsDown(StateInfo info)
        {
            for (int i = 0; i <= 3; i++)
            {
                client.SetState(info.ID, i);
                Thread.Sleep(500);
            }
        }

        void FlapsUp(StateInfo info)
        {
            for (int i = 3; i >= 0; i--)
            {
                client.SetState(info.ID, i);
                Thread.Sleep(500);
            }
        }

    }

    public enum GearState
    {
        Unknown,
        Down,
        Up,
        Moving,
        MovingDown,
        MovingUp
    }
}
