using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Xml.Linq;
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
        
        private bool _isSaxSelected;
        private bool _isDomSelected;
        private bool _isLinqSelected = true;

        public ObservableCollection<Software> SoftwareList { get; set; } = new ObservableCollection<Software>();
        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();

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
        private string _statusMessage = "Файл не обрано";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

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
            
                    // !!! ОСЬ ЦЕЙ РЯДОК ЗМІНЮЄ ТЕКСТ НА ЕКРАНІ !!!
                    StatusMessage = $"Обрано: {result.FileName}"; 
            
                    // Видаліть або закоментуйте DisplayAlert, щоб не дратував
                    // await Application.Current!.MainPage!.DisplayAlert("Успіх", ...);

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

                foreach (var cat in cats)
                {
                    Categories.Add(cat!);
                }
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

            // Вибір стратегії
            if (IsSaxSelected) strategy = new SaxSearchStrategy();
            else if (IsDomSelected) strategy = new DomSearchStrategy();
            else strategy = new LinqSearchStrategy();

            var results = strategy.Search(_filePath, SearchKeyword, SelectedCategory);

            foreach (var item in results)
            {
                SoftwareList.Add(item);
            }
            
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
            if (string.IsNullOrEmpty(_filePath)) return;

            try
            {
                string xslPath = Path.Combine(FileSystem.CacheDirectory, "transform.xsl");
                string htmlPath = Path.Combine(FileSystem.CacheDirectory, "output.html");
                
                File.WriteAllText(xslPath, XslContent);

                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(xslPath);
                transform.Transform(_filePath, htmlPath);

                bool open = await Application.Current!.MainPage!.DisplayAlert("Успіх", $"HTML готовий. Відкрити?", "Так", "Ні");
                
                if(open)
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(htmlPath) });
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Помилка", ex.Message, "ОК");
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
      <body style='font-family: Arial;'>
        <h2>Звіт: Програмне забезпечення</h2>
        <table border='1' cellpadding='5' style='border-collapse: collapse; width: 100%;'>
          <tr bgcolor='#f2f2f2'>
            <th>Категорія</th>
            <th>Назва</th>
            <th>Автор</th>
            <th>Деталі</th>
          </tr>
          <xsl:for-each select='FacultyNetwork/Category/Software'>
            <tr>
              <td><xsl:value-of select='../@Name'/></td>
              <td><xsl:value-of select='@Name'/></td>
              <td><xsl:value-of select='@Author'/></td>
              <td>
                <xsl:if test='@LicenseKey'>Key: <xsl:value-of select='@LicenseKey'/></xsl:if>
                <xsl:if test='@RepoUrl'>Repo: <xsl:value-of select='@RepoUrl'/></xsl:if>
              </td>
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