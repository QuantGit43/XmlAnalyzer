using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.Serialization; // Для збереження результатів
using System.Xml.Xsl;
using XmlAnalyzer.Models;
using XmlAnalyzer.Services;

namespace XmlAnalyzer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string? _filePath;
        private string? _selectedCategory;
        private string? _searchKeyword;
        private string _statusMessage = "Файл не обрано";
        
        private bool _isSaxSelected;
        private bool _isDomSelected;
        private bool _isLinqSelected = true;

        public ObservableCollection<Software> SoftwareList { get; set; } = new ObservableCollection<Software>();
        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string? SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public string? SearchKeyword
        {
            get => _searchKeyword;
            set { _searchKeyword = value; OnPropertyChanged(); }
        }

        public bool IsSaxSelected
        {
            get => _isSaxSelected;
            set { _isSaxSelected = value; OnPropertyChanged(); }
        }

        public bool IsDomSelected
        {
            get => _isDomSelected;
            set { _isDomSelected = value; OnPropertyChanged(); }
        }

        public bool IsLinqSelected
        {
            get => _isLinqSelected;
            set { _isLinqSelected = value; OnPropertyChanged(); }
        }

        public ICommand LoadFileCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ExitCommand { get; }

        public MainViewModel()
        {
            LoadFileCommand = new Command(async () => await LoadFile());
            SearchCommand = new Command(PerformSearch);
            ClearCommand = new Command(ClearFields);
            ExportCommand = new Command(async () => await ExportToHtml());
            ExitCommand = new Command(async () => await ConfirmExit());
        }

        private async Task LoadFile()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    _filePath = result.FullPath;
                    StatusMessage = $"XML: {result.FileName}";
                    LoadCategoriesFromFile();
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Помилка", ex.Message, "ОК");
            }
        }

        private void LoadCategoriesFromFile()
        {
            if (string.IsNullOrEmpty(_filePath)) return;
            Categories.Clear();
            try
            {
                var doc = XDocument.Load(_filePath);
                var cats = doc.Descendants("Category")
                              .Select(c => c.Attribute("Name")?.Value)
                              .Where(n => n != null)
                              .Distinct()
                              .ToList();
                foreach (var cat in cats) Categories.Add(cat!);
            }
            catch { }
        }

        private void PerformSearch()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                Application.Current!.MainPage!.DisplayAlert("Увага", "Спочатку завантажте файл!", "ОК");
                return;
            }

            SoftwareList.Clear();
            ISearchStrategy strategy;

            // Патерн Стратегія
            if (IsSaxSelected) strategy = new SaxSearchStrategy();
            else if (IsDomSelected) strategy = new DomSearchStrategy();
            else strategy = new LinqSearchStrategy();

            var results = strategy.Search(_filePath, SearchKeyword, SelectedCategory);

            foreach (var item in results) SoftwareList.Add(item);
            
            if (results.Count == 0)
                 Application.Current!.MainPage!.DisplayAlert("Інфо", "Нічого не знайдено", "ОК");
        }

        private void ClearFields()
        {
            SearchKeyword = string.Empty;
            SelectedCategory = null;
            SoftwareList.Clear();
        }

        private async Task ExportToHtml()
        {
            if (SoftwareList.Count == 0)
            {
                await Application.Current!.MainPage!.DisplayAlert("Увага", "Спочатку виконайте пошук!", "ОК");
                return;
            }

            try
            {
                string tempXmlPath = Path.Combine(FileSystem.CacheDirectory, "temp_data.xml");
                string tempXslPath = Path.Combine(FileSystem.CacheDirectory, "style.xsl");
                string htmlPath = Path.Combine(FileSystem.CacheDirectory, "output.html");

               File.WriteAllText(tempXslPath, XslContent);

                XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Software>));
                using (StreamWriter writer = new StreamWriter(tempXmlPath))
                {
                    serializer.Serialize(writer, SoftwareList);
                }

                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(tempXslPath);
                transform.Transform(tempXmlPath, htmlPath);
                
                bool open = await Application.Current!.MainPage!.DisplayAlert("Успіх", "Звіт готовий. Відкрити?", "Так", "Ні");
                if (open)
                {
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(htmlPath) });
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Помилка експорту", ex.Message, "ОК");
            }
        }

        private async Task ConfirmExit()
        {
            bool answer = await Application.Current!.MainPage!.DisplayAlert("Вихід", "Завершити роботу?", "Так", "Ні");
            if (answer) Application.Current.Quit();
        }

        private const string XslContent = @"<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
  <xsl:template match='/'>
    <html>
      <body style='font-family: Segoe UI, Arial; padding: 20px; background-color: #f9f9f9;'>
        <h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>Результати пошуку ПЗ</h2>
        <table border='1' cellpadding='12' style='border-collapse: collapse; width: 100%; box-shadow: 0 0 20px rgba(0,0,0,0.1); background-color: white;'>
          <tr style='background-color: #3498db; color: white; text-align: left;'>
            <th>Категорія</th>
            <th>Назва</th>
            <th>Автор</th>
            <th>Додаткова інформація</th>
          </tr>
          <xsl:for-each select='ArrayOfSoftware/Software'>
            <tr style='border-bottom: 1px solid #ddd;'>
              <td style='font-weight: bold; color: #555;'><xsl:value-of select='@Category'/></td>
              <td style='font-size: 1.1em;'><xsl:value-of select='@Name'/></td>
              <td><xsl:value-of select='@Author'/></td>
              <td style='color: #666;'><xsl:value-of select='@Description'/></td>
            </tr>
          </xsl:for-each>
        </table>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}