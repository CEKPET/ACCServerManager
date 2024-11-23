using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ACCServerManager
{
    public partial class MainWindow : Window
    {
        private List<Track> tracks = new List<Track>();
        private Track selectedTrack = null;
        private List<Session> sessions = new List<Session>();

        private string eventJsonPath;

        public MainWindow()
        {
            InitializeComponent();

            // Загружаем путь из настроек
            LoadEventJsonPath();

            // Если путь не задан, просим пользователя указать
            if (string.IsNullOrEmpty(eventJsonPath) || !File.Exists(Path.Combine(eventJsonPath, "event.json")))
            {
                MessageBox.Show("Не указан путь к файлу event.json. Выберите папку.", "Внимание");
                SelectEventJsonPath();
            }

            // Пытаемся загрузить данные из файла
            LoadEventJson();
            
            LoadTracks();

            TrackListControl.SelectedIndex = -1;
        }
        private void LoadEventJsonPath()
        {
            eventJsonPath = Properties.Settings.Default.EventJsonPath;

            if (!string.IsNullOrEmpty(eventJsonPath) && Directory.Exists(eventJsonPath))
            {
                // Проверяем наличие файла
                string fullPath = Path.Combine(eventJsonPath, "event.json");
                if (!File.Exists(fullPath))
                {
                    MessageBox.Show($"Файл event.json не найден по пути {eventJsonPath}", "Ошибка");
                }
            }
        }
        private void LoadEventJson()
        {
            try
            {
                string fullPath = Path.Combine(eventJsonPath, "event.json");
                //MessageBox.Show($"Путь к event.json: {fullPath}");

                if (File.Exists(fullPath))
                {
                    string json = File.ReadAllText(fullPath);
                    //MessageBox.Show($"JSON содержимое event.json:\n{json}");

                    var eventConfig = JsonConvert.DeserializeObject<EventConfig>(json);
                    PopulateForm(eventConfig); // Загрузка данных в форму
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки event.json: {ex.Message}");
            }
        }

        private void SelectEventJsonPath()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Выберите папку с event.json"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                eventJsonPath = dialog.FileName;
                Properties.Settings.Default.EventJsonPath = eventJsonPath;
                Properties.Settings.Default.Save();
            }
            else
            {
                MessageBox.Show("Путь не был выбран. Приложение не может продолжить.", "Ошибка");
                Close(); // Закрываем приложение, если путь не задан
            }
        }

        

        private void PopulateForm(EventConfig config)
        {
            PreRaceWaitingTimeTextBox.Text = config.PreRaceWaitingTimeSeconds.ToString();
            SessionOverTimeTextBox.Text = config.SessionOverTimeSeconds.ToString();
            AmbientTempTextBox.Text = config.AmbientTemp.ToString();
            CloudLevelComboBox.SelectedIndex = (int)(config.CloudLevel * 10); // Пример перевода
            WeatherRandomnessComboBox.SelectedIndex = config.WeatherRandomness;
            RainComboBox.SelectedIndex = config.Rain > 0.5 ? 2 : 0; // Пример перевода

            SessionsListBox.Items.Clear();
            foreach (var session in config.Sessions)
            {
                SessionsListBox.Items.Add($"Сессия: {session.SessionType}, {session.SessionDurationMinutes} минут");
            }
        }

        private void LoadTracks()
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "tracks.json");
                //MessageBox.Show($"Путь к tracks.json: {filePath}");

                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Файл tracks.json не найден!");
                    return;
                }

                string json = File.ReadAllText(filePath);
                //MessageBox.Show($"JSON содержимое:\n{json}");

                tracks = JsonConvert.DeserializeObject<List<Track>>(json);

                if (tracks == null || tracks.Count == 0)
                {
                    MessageBox.Show("Список трасс пуст.");
                    return;
                }

                TrackListControl.ItemsSource = tracks; // Привязка данных
                //MessageBox.Show($"Загружено {tracks.Count} трасс.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки трасс: {ex.Message}");
            }
        }

        private void ConfirmTrackSelection_Click(object sender, RoutedEventArgs e)
        {
            if (TrackListControl.SelectedItem is Track track)
            {
                selectedTrack = track;

                TrackNameText.Text = $"Трасса: {track.Name}";
                TrackTurnsText.Text = $"Повороты: {track.Turns}";
                TrackRecordText.Text = $"Рекорд круга: {track.Record}";

                try
                {
                    // Проверяем, существует ли изображение
                    if (File.Exists(track.ImagePath))
                    {
                        string fullPath = Path.GetFullPath(track.ImagePath);
                        TrackImage.Source = new BitmapImage(new Uri(fullPath, UriKind.Absolute));
                    }
                    else
                    {
                        TrackImage.Source = null;
                        MessageBox.Show($"Изображение не найдено: {track.ImagePath}");
                    }
                }
                catch (Exception ex)
                {
                    TrackImage.Source = null;
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }

                TrackSliderPanel.Visibility = Visibility.Collapsed;
                TrackCardPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Выберите трассу.");
            }
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

        private void ShowTrackSlider_Click(object sender, RoutedEventArgs e)
        {
            TrackSliderPanel.Visibility = Visibility.Visible;
            TrackCardPanel.Visibility = Visibility.Collapsed;
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

        private void ChangePathMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectEventJsonPath(); // Метод для выбора пути к файлу
            LoadEventJson();       // Перезагрузка данных из нового пути
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
                    Track = selectedTrack?.ID,
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

    public class Track
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public int Turns { get; set; }
        public string Record { get; set; }
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

    public class SelectionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return Brushes.LightBlue; // Цвет выделения
            }
            return Brushes.White; // Цвет по умолчанию
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
