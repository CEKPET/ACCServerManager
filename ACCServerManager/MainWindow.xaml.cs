using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            if (!int.TryParse(SessionDurationTextBox.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Введите корректную длительность сессии!");
                return;
            }

            var session = new Session
            {
                HourOfDay = int.Parse(((ComboBoxItem)HourOfDayComboBox.SelectedItem).Content.ToString()),
                DayOfWeekend = DayOfWeekendComboBox.SelectedIndex + 1,
                TimeMultiplier = int.Parse(((ComboBoxItem)TimeMultiplierComboBox.SelectedItem).Content.ToString()),
                SessionType = ((ComboBoxItem)SessionTypeComboBox.SelectedItem).Content.ToString()[0].ToString(),
                SessionDurationMinutes = duration
            };

            sessions.Add(session);
            SessionsListBox.Items.Add($"Сессия: {session.SessionType}, {session.SessionDurationMinutes} минут");
        }

        private void RemoveSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SessionsListBox.SelectedIndex >= 0)
            {
                sessions.RemoveAt(SessionsListBox.SelectedIndex);
                SessionsListBox.Items.RemoveAt(SessionsListBox.SelectedIndex);
            }
        }


        private void GenerateJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация и извлечение значений
                if (!int.TryParse(PreRaceWaitingTimeTextBox.Text, out int preRaceWaitingTime) || preRaceWaitingTime < 30)
                {
                    MessageBox.Show("Введите корректное значение для времени ожидания (>= 30 секунд).");
                    return;
                }

                if (!int.TryParse(SessionOverTimeTextBox.Text, out int sessionOverTime) || sessionOverTime < 180)
                {
                    MessageBox.Show("Введите корректное значение для времени завершения сессии (>= 180 секунд).");
                    return;
                }

                if (!int.TryParse(AmbientTempTextBox.Text, out int ambientTemp) || ambientTemp < 5 || ambientTemp > 40)
                {
                    MessageBox.Show("Введите корректное значение для температуры (от 5 до 40°C).");
                    return;
                }

                if (CloudLevelComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите значение для уровня облачности.");
                    return;
                }

                if (RainComboBox.SelectedIndex < 0)
                {
                    MessageBox.Show("Выберите значение для дождя.");
                    return;
                }

                if (WeatherRandomnessComboBox.SelectedIndex < 0)
                {
                    MessageBox.Show("Выберите значение для случайности погоды.");
                    return;
                }

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
                // Собираем данные для JSON
                var eventConfig = new EventConfig
                {
                    Track = TrackComboBox.SelectedItem?.ToString(),
                    PreRaceWaitingTimeSeconds = preRaceWaitingTime,
                    SessionOverTimeSeconds = sessionOverTime,
                    AmbientTemp = ambientTemp,

                    // Проверяем и преобразуем CloudLevel
                    CloudLevel = CloudLevelComboBox.SelectedItem is ComboBoxItem cloudLevelItem
    && double.TryParse(
        cloudLevelItem.Content.ToString(),
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out double cloudLevel)
        ? cloudLevel
        : throw new FormatException("Некорректное значение уровня облачности."),

                    // Устанавливаем значение Rain
                    Rain = rain,

                    WeatherRandomness = WeatherRandomnessComboBox.SelectedIndex >= 0
                        ? WeatherRandomnessComboBox.SelectedIndex
                        : throw new InvalidOperationException("Не выбрана случайность погоды."),

                    Sessions = sessions,
                    ConfigVersion = 1
                };

                // Создаем настройки для сериализации
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented, // Читаемый формат
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy() // Все ключи в нижнем регистре
                    }
                };

                // Генерация JSON
                string json = JsonConvert.SerializeObject(eventConfig, Formatting.Indented);
                File.WriteAllText("event.json", json);

                MessageBox.Show("JSON файл успешно создан!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации JSON: {ex.Message}");
            }
        }


        public class EventConfig
        {
            [JsonProperty("track")]
            public string Track { get; set; }

            [JsonProperty("preRaceWaitingTimeSeconds")]
            public int PreRaceWaitingTimeSeconds { get; set; }

            [JsonProperty("sessionOverTimeSeconds")]
            public int SessionOverTimeSeconds { get; set; }

            [JsonProperty("ambientTemp")]
            public int AmbientTemp { get; set; }

            [JsonProperty("cloudLevel")]
            public double CloudLevel { get; set; }

            [JsonProperty("rain")]
            public double Rain { get; set; }

            [JsonProperty("weatherRandomness")]
            public int WeatherRandomness { get; set; }

            [JsonProperty("sessions")]
            public List<Session> Sessions { get; set; }

            [JsonProperty("configVersion")]
            public int ConfigVersion { get; set; }
        }

        public class Session
        {
            [JsonProperty("hourOfDay")]
            public int HourOfDay { get; set; }

            [JsonProperty("dayOfWeekend")]
            public int DayOfWeekend { get; set; }

            [JsonProperty("timeMultiplier")]
            public int TimeMultiplier { get; set; }

            [JsonProperty("sessionType")]
            public string SessionType { get; set; }

            [JsonProperty("sessionDurationMinutes")]
            public int SessionDurationMinutes { get; set; }
        }
    }
}
