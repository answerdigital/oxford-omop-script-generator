using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace oxford_import_script_generator;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Generating omop script.");

        Configuration? configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText("appsettings.json"));

        var cdsFiles = GetDirectoryFiles(configuration!.PathToCdsDirectory!, "cds", GetCdsDate);
        var cosdFiles = GetDirectoryFiles(configuration.PathToCosdDirectory!, "cosd", GetCosdDate);
        var sactFiles = GetSactFiles(configuration!);
        var rtdsFiles = GetRtdsFiles(configuration!);

        var files =
            cdsFiles
                .Concat(cosdFiles)
                .Concat(sactFiles)
                .Concat(rtdsFiles)
                .ToList();

        GenerateAndWriteScript(configuration, files, stageOnly: true);
        GenerateAndWriteScript(configuration, files, stageOnly: false);
    }

    private static IEnumerable<DataFile> GetSactFiles(Configuration configuration)
    {
        var files = new List<DataFile>();

        foreach (var directory in Directory.GetDirectories(configuration.PathToSactDirectory!))
        {
            if (int.TryParse(Path.GetFileName(directory), out _)) // Check inside the 2020, 2021, 2022 directories etc
            {
                files.AddRange(GetDirectoryFiles(directory, "sact", GetSactDate));
            }
        }

        return files;
    }
    
    private static DateTime? GetSactDate(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        // expected format "SACT_v3-20230101-20230131.csv"

        string dateText = fileName.Substring(8, 8);

        return ParseShortDate(dateText);
    }

    private static IEnumerable<DataFile> GetRtdsFiles(Configuration configuration)
    {
        var files = new List<DataFile>();

        foreach (var directory in Directory.GetDirectories(configuration.PathToRtdsDirectory!))
        {
            files.AddRange(GetDirectoryFiles(directory!, "rtds", GetRtdsDate));
        }

        return files;
    }

    private static DateTime? GetRtdsDate(string path)
    {
        var fileName = Path.GetFileName(path);

        var dateTime = ParseShortDate(fileName!);

        if (dateTime != null)
            return dateTime;

        // Otherwise we must be in this format "RTH_RTDS_Apr_2020_6.zip."

        var wordedMonth = GetWordedMonthOfYearFromFileName(fileName);
        
        if (wordedMonth == null)
        {
            return null; // Give up.
        }

        var wordedYear = GetFourDigitYearFromFileName(fileName) ?? GetTwoDigitYearFromFileName(fileName);

        if (wordedYear == null)
        {
            return null;
        }
        
        return new DateTime(wordedYear.Value, wordedMonth.Value, 1);
    }

    private static IEnumerable<DataFile> GetDirectoryFiles (string path, string type, Func<string, DateTime?> dateExtractor)
    {
        foreach (var file in Directory.GetFiles(path).Concat(Directory.GetDirectories(path)))
        {
            Console.WriteLine($"Parsing {file}");

            DateTime? date = dateExtractor(file);

            if (date == null)
            {
                Console.Error.WriteLine($"Cannot parse date from {type} filename {file}.");
                date = DateTime.MinValue;
            }

            yield return new DataFile { DateTime = date.Value, Path = file, Type = type };
        }
    }

    private static DateTime? GetCosdDate(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        var wordMonth = GetWordedMonthOfYearFromFileName(fileName);

        if (wordMonth == null)
        {
            // expected format "COSD_INFOFLEX(CT)_RTH_2020-04-01_2020-04-30_2020-06-01T10_28_19.zip"

            if (fileName.Length < 35) // Must be some other format
                return null;

            var dateTimeText = fileName.Substring(22, 10);
            
            if (DateTime.TryParse(dateTimeText, out var parsedDate))
            {
                return parsedDate;
            }

            return null; // Can't parse it, give up.
        }
        else
        {
            // expected format "March 2023 Submission Final.zip"

            var wordedYear = GetFourDigitYearFromFileName(filePath) ?? GetTwoDigitYearFromFileName(fileName);

            if (wordedYear == null) 
                return null; // Give up.

            return new DateTime(wordedYear.Value, wordMonth.Value, 1);
        }
    }

    private static int? GetWordedMonthOfYearFromFileName(string fileName)
    {
        var match = Regex.Matches(fileName, "(January|Jan|February|Feb|March|Mar|April|Apr|May|June|Jun|July|Jul|August|Aug|September|Sep|October|Oct|November|Nov|December|Dec)", RegexOptions.IgnoreCase);

        if (match.Any())
        {
            var months =
                new []
                {
                    new KeyValuePair<string, int>("January", 1),
                    new KeyValuePair<string, int>("Jan", 1),
                    new KeyValuePair<string, int>("February", 2),
                    new KeyValuePair<string, int>("Feb", 2),
                    new KeyValuePair<string, int>("March", 3),
                    new KeyValuePair<string, int>("Mar", 3),
                    new KeyValuePair<string, int>("April", 4),
                    new KeyValuePair<string, int>("Apr", 4),
                    new KeyValuePair<string, int>("May", 5),
                    new KeyValuePair<string, int>("June", 6),
                    new KeyValuePair<string, int>("Jun", 6),
                    new KeyValuePair<string, int>("July", 7),
                    new KeyValuePair<string, int>("Jul", 7),
                    new KeyValuePair<string, int>("August", 8),
                    new KeyValuePair<string, int>("Aug", 8),
                    new KeyValuePair<string, int>("September", 9),
                    new KeyValuePair<string, int>("Sep", 9),
                    new KeyValuePair<string, int>("October", 10),
                    new KeyValuePair<string, int>("Oct", 10),
                    new KeyValuePair<string, int>("November", 11),
                    new KeyValuePair<string, int>("Nov", 11),
                    new KeyValuePair<string, int>("December", 12),
                    new KeyValuePair<string, int>("Dec", 12),
                };

            string monthText = match[0].Groups[1].Value;

            var monthMatch = months.Cast<KeyValuePair<string, int>?>().FirstOrDefault(m => m.Value.Key.Equals(monthText, StringComparison.CurrentCultureIgnoreCase));

            return monthMatch?.Value;
        }

        return null;
    }

    private static int? GetFourDigitYearFromFileName(string fileName)
    {
        var match = Regex.Matches(fileName, "[ _](2\\d{3})[_\\. ]");

        if (match.Any())
        {
            return int.Parse(match[0].Groups[1].Value);
        }

        return null;
    }

    private static int? GetTwoDigitYearFromFileName(string fileName)
    {
        var match = Regex.Matches(fileName, "[ _](\\d{2})[_ -\\.]");

        if (match.Any())
        {
            return 2000 + int.Parse(match[0].Groups[1].Value);
        }

        return null;
    }

    private static DateTime? GetCdsDate(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        string[] nameParts = fileName.Split('_');

        if (nameParts.Length == 1)
            return null;

        return ParseShortDate(nameParts.Last()[..8]);
    }

    private static DateTime? ParseShortDate(string text)
    {
        if (DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }

        return null;
    }

    private static void GenerateAndWriteScript(Configuration configuration, IReadOnlyCollection<DataFile> files, bool stageOnly)
    {
        StringBuilder stringBuilder = new StringBuilder();

        // Clear staging tables.
        stringBuilder.AppendLine("# Clear staging tables.");

        foreach (var type in new[] { "cds", "rtds", "sact", "cosd"})
        {
            stringBuilder.AppendLine($".\\{configuration.OmopToolPath} stage clear --type {type}");
        }

        stringBuilder.AppendLine("# Stage and transform.");

        int count = 1;

        foreach (var file in files.OrderBy(file => file.DateTime))
        {
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine($"Write-Output \"Importing item {count} of {files.Count}. ({file.DateTime})\"");
            stringBuilder.AppendLine($"# {file.DateTime}");
            stringBuilder.AppendLine($".\\{configuration.OmopToolPath} stage load --type {file.Type} \"{file.Path}\"");

            if (stageOnly == false)
            {
                stringBuilder.AppendLine($".\\{configuration.OmopToolPath} transform --type {file.Type}");
                stringBuilder.AppendLine($".\\{configuration.OmopToolPath} stage clear --type {file.Type}");
            }

            count++;
        }

        if (stageOnly == false)
        {
            stringBuilder.AppendLine($".\\{configuration.OmopToolPath} purge");
        }

        string fileName = stageOnly ? "stage-only.ps1" : "stage-and-transform.ps1";

        File.WriteAllText(Path.Combine(configuration.OutputPath!, fileName), stringBuilder.ToString());
    }
}

internal class DataFile
{
    public string Path { get; init; }
    public string Type { get; init; }
    public DateTime DateTime { get; init; }
}

internal class Configuration
{
    public string? PathToCdsDirectory { get; set; }
    public string? PathToCosdDirectory { get; set; }
    public string? PathToSactDirectory { get; set; }
    public string? PathToRtdsDirectory { get; set; }
    public string? OmopToolPath { get; set; }
    public string? OutputPath { get; set; }
}