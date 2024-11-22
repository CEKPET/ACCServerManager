using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ACCServerManager
{
    public partial class MainWindow : Window
    {
        private List<Session> sessions = new List<Session>();

        public MainWindow()
        {
            InitializeComponent();
            LoadTracks();
        }

        private void LoadTracks()
        {
            var tracks = new List<string>
            {
                "barcelona", "brands_hatch", "hungaroring", "misano", "monza",
                "nurburgring", "paul_ricard", "silverstone", "spa", "zandvoort", "zolder"
            };

            TrackComboBox.ItemsSource = tracks;
        }

        private void PreRaceWaitingTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PreRaceWaitingTimeTextBox.Text = ((int)e.NewValue).ToString();
        }

        private void SessionOverTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SessionOverTimeTextBox.Text = ((int)e.NewValue).ToString();
        }

        private void AmbientTempSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AmbientTempTextBox.Text = ((int)e.NewValue).ToString();
        }

        private void AddSessionButton_Click(object sender, RoutedEventArgs e)
        {
            var session = new Session
            {
                HourOfDay = 12,
                DayOfWeekend = 2,
                TimeMultiplier = 1,
                SessionType = "P",
                SessionDurationMinutes = 15
            };

            sessions.Add(session);
            SessionsListBox.Items.Add($"Сессия {session.SessionType} - {session.SessionDurationMinutes} минут");
        }

        private void GenerateJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double rain;
                switch (RainComboBox.SelectedIndex)
                {
                    case 0: rain = 0.0; break;
                    case 1: rain = 0.2; break;
                    case 2: rain = 0.5; break;
                    case 3: rain = 0.7; break;
                    case 4: rain = 1.0; break;
                    default: rain = 0.0; break;
                }
                var eventConfig = new EventConfig
                {
                    Track = TrackComboBox.SelectedItem?.ToString() ?? "zolder",
                    PreRaceWaitingTimeSeconds = int.Parse(PreRaceWaitingTimeTextBox.Text),
                    SessionOverTimeSeconds = int.Parse(SessionOverTimeTextBox.Text),
                    AmbientTemp = int.Parse(AmbientTempTextBox.Text),
                    CloudLevel = double.Parse(CloudLevelComboBox.Text),
                    Rain = rain,
                    WeatherRandomness = WeatherRandomnessComboBox.SelectedIndex,
                    Sessions = sessions,
                    ConfigVersion = 1
                };

                string json = JsonConvert.SerializeObject(eventConfig, Formatting.Indented);
                File.WriteAllText("event.json", json);
                MessageBox.Show("JSON файл успешно создан!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации JSON: {ex.Message}");
            }
        }
    }

    public class EventConfig
    {
        public string Track { get; set; }
        public int PreRaceWaitingTimeSeconds { get; set; }
        public int SessionOverTimeSeconds { get; set; }
        public int AmbientTemp { get; set; }
        public double CloudLevel { get; set; }
        public double Rain { get; set; }
        public int WeatherRandomness { get; set; }
        public List<Session> Sessions { get; set; }
        public int ConfigVersion { get; set; }
    }

    public class Session
    {
        public int HourOfDay { get; set; }
        public int DayOfWeekend { get; set; }
        public int TimeMultiplier { get; set; }
        public string SessionType { get; set; }
        public int SessionDurationMinutes { get; set; }
    }
}
