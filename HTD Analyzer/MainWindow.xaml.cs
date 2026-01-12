using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

#nullable enable

namespace HiddenTextDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Finding> _allFindings = new List<Finding>();
        private List<Finding> _filteredFindings = new List<Finding>();
        private string? _selectedFilePath = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Browse for a document file
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All Documents|*.pdf;*.docx;*.xlsx;*.xls|PDF|*.pdf|Word|*.docx|Excel|*.xlsx;*.xls|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedFilePath = dialog.FileName;
                FilePathTextBox.Text = Path.GetFileName(_selectedFilePath);
                FilePathTextBox.Foreground = System.Windows.Media.Brushes.Black;
                AnalyzeButton.IsEnabled = true;
                StatusTextBlock.Text = "Ready to analyze";
            }
        }

        /// <summary>
        /// Analyze the selected document for hidden text
        /// </summary>
        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("Please select a file first.", "No File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AnalyzeButton.IsEnabled = false;
            StatusTextBlock.Text = "Analyzing...";

            try
            {
                var result = await Task.Run(() => RunAnalyzer(_selectedFilePath));

                if (!string.IsNullOrEmpty(result.Error))
                {
                    MessageBox.Show(result.Error, "Analysis Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _allFindings.Clear();
                    _filteredFindings.Clear();
                    FindingsDataGrid.ItemsSource = null;
                    TotalFindingsValue.Text = "0";
                    ReasonStatsControl.ItemsSource = null;
                }
                else
                {
                    _allFindings = result.Findings ?? new List<Finding>();
                    ApplyFilters();
                    TotalFindingsValue.Text = result.TotalFindings.ToString();
                    ExportButton.IsEnabled = _allFindings.Count > 0;
                    NewDocumentButton.IsEnabled = true;
                    StatusTextBlock.Text = $"Found {result.TotalFindings} hidden text instances";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AnalyzeButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Run the Python analyzer executable and parse results
        /// </summary>
        private AnalysisResult RunAnalyzer(string? filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return new AnalysisResult { Error = "No file path provided" };
                }
                // Use relative path - "document_analyzer.exe" should be in same folder as app
                string analyzerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "document_analyzer.exe");

                if (!File.Exists(analyzerPath))
                {
                    return new AnalysisResult { Error = $"Analyzer not found at {analyzerPath}" };
                }

                var psi = new ProcessStartInfo
                {
                    FileName = analyzerPath,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                        return new AnalysisResult { Error = "Failed to start analyzer process" };

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (string.IsNullOrEmpty(output))
                        return new AnalysisResult { Error = "No output from analyzer - file may not contain hidden text" };

                    // Parse JSON using System.Text.Json
                    AnalysisResult result = new AnalysisResult();
                    try
                    {
                        //System.Text.Json.JsonDocument.Parse ----> potentially dangerous
                        using (JsonDocument doc = JsonDocument.Parse(output))
                        {
                            // Check for error in root
                            if (doc.RootElement.TryGetProperty("error", out JsonElement errorElement))
                            {
                                result.Error = errorElement.GetString() ?? string.Empty;
                            }

                            // Deserialize the remaining fields
                            result.File = doc.RootElement.GetProperty("file").GetString() ?? string.Empty;
                            result.FileType = doc.RootElement.GetProperty("file_type").GetString() ?? string.Empty;
                            result.TotalFindings = doc.RootElement.GetProperty("total_findings").GetInt32();

                            // Deserialize findings
                            result.Findings = new List<Finding>();
                            if (doc.RootElement.TryGetProperty("findings", out JsonElement findingsElement) && findingsElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in findingsElement.EnumerateArray())
                                {
                                    var f = new Finding();
                                    if (item.TryGetProperty("page", out var p)) f.Page = p.GetInt32();
                                    if (item.TryGetProperty("location", out var loc)) f.Location = loc.GetString() ?? string.Empty;
                                    if (item.TryGetProperty("text", out var t)) f.Text = t.GetString() ?? string.Empty;
                                    if (item.TryGetProperty("font_name", out var fn)) f.FontName = fn.GetString() ?? string.Empty;
                                    if (item.TryGetProperty("font_size", out var fsz)) f.FontSize = fsz.GetDouble();
                                    if (item.TryGetProperty("color_rgb", out var cr)) f.ColorRgb = cr.GetString() ?? string.Empty;
                                    if (item.TryGetProperty("hidden_reasons", out var hr) && hr.ValueKind == JsonValueKind.Array)
                                    {
                                        f.HiddenReasons = hr.EnumerateArray()
                                            .Select(x => x.GetString())
                                            .Where(s => s != null)
                                            .Select(s => s!)
                                            .ToList();
                                    }
                                    result.Findings.Add(f);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return new AnalysisResult { Error = $"Failed to parse analyzer output: {ex.Message}" };
                    }

                    // Process findings to join hidden reasons
                    if (result?.Findings != null)
                    {
                        foreach (var finding in result.Findings)
                        {
                            if (finding.HiddenReasons != null && finding.HiddenReasons.Count > 0)
                            {
                                finding.HiddenReasonsJoined = string.Join(" | ", finding.HiddenReasons);
                            }
                            else
                            {
                                finding.HiddenReasonsJoined = "Unknown";
                            }
                        }
                    }

                    return result ?? new AnalysisResult { Error = "Failed to parse analyzer output" };
                }
            }
            catch (Exception ex)
            {
                return new AnalysisResult { Error = $"Analyzer error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Apply all active filters to the findings
        /// </summary>
        private void ApplyFilters()
        {
            _filteredFindings = _allFindings.ToList();

            // Filter by hidden reason
            var reasonComboBox = HiddenReasonFilter.SelectedItem as ComboBoxItem;
            var reasonFilter = reasonComboBox?.Content?.ToString() ?? "All Reasons";

            if (reasonFilter != "All Reasons")
            {
                _filteredFindings = _filteredFindings
                    .Where(f => (f.HiddenReasons != null && f.HiddenReasons.Any(r => r.IndexOf(reasonFilter, StringComparison.OrdinalIgnoreCase) >= 0)))
                    .ToList();
            }

            // Filter by file type
            var typeComboBox = FileTypeFilter.SelectedItem as ComboBoxItem;
            var typeFilter = typeComboBox?.Content?.ToString() ?? "All Types";

            if (typeFilter != "All Types")
            {
                _filteredFindings = _filteredFindings
                    .Where(f => f.Location != null && f.Location.Contains(typeFilter))
                    .ToList();
            }

            // Filter by search text
            var searchText = SearchTextBox.Text ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                var lowerSearch = searchText.ToLower();
                _filteredFindings = _filteredFindings
                    .Where(f => (f.Text != null && f.Text.ToLower().IndexOf(lowerSearch, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (f.Location != null && f.Location.ToLower().IndexOf(lowerSearch, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }

            // Update DataGrid
            FindingsDataGrid.ItemsSource = null; // Reset to refresh
            FindingsDataGrid.ItemsSource = _filteredFindings;

            // Update statistics
            UpdateStatistics();
        }

        /// <summary>
        /// Update statistics panel with hidden reason breakdown
        /// </summary>
        private void UpdateStatistics()
        {
            var reasonStats = _filteredFindings
                .Where(f => f.HiddenReasons != null && f.HiddenReasons.Count > 0)
                .SelectMany(f => f.HiddenReasons)
                .GroupBy(r => r)
                .Select(g => new { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            ReasonStatsControl.ItemsSource = null; // Reset to refresh
            ReasonStatsControl.ItemsSource = reasonStats;
        }

        /// <summary>
        /// Handle filter changes
        /// </summary>
        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// Handle search text changes
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            HiddenReasonFilter.SelectedIndex = 0;
            FileTypeFilter.SelectedIndex = 0;
            SearchTextBox.Text = "";
            ApplyFilters();
        }

        /// <summary>
        /// Load a new document
        /// </summary>
        private void NewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedFilePath = "";
            _allFindings.Clear();
            _filteredFindings.Clear();
            FilePathTextBox.Text = "Select a file...";
            FilePathTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            FindingsDataGrid.ItemsSource = null;
            TotalFindingsValue.Text = "0";
            ReasonStatsControl.ItemsSource = null;
            AnalyzeButton.IsEnabled = false;
            ExportButton.IsEnabled = false;
            NewDocumentButton.IsEnabled = false;
            StatusTextBlock.Text = "Ready";
        }

        /// <summary>
        /// Export findings to CSV file
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredFindings.Count == 0)
            {
                MessageBox.Show("No findings to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                FileName = $"hidden_text_findings_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        // Write header
                        writer.WriteLine("Location,Text,Font Name,Font Size (pt),Color (RGB),Hidden Reason");

                        // Write data rows
                        foreach (var finding in _filteredFindings)
                        {
                            var location = EscapeCsvField(finding.Location ?? "");
                            var text = EscapeCsvField(finding.Text ?? "");
                            var fontName = EscapeCsvField(finding.FontName ?? "");
                            var fontSize = finding.FontSize?.ToString("F1") ?? "";
                            var color = EscapeCsvField(finding.ColorRgb ?? "");
                            var reason = EscapeCsvField(finding.HiddenReasonsJoined ?? "");

                            writer.WriteLine($"{location},{text},{fontName},{fontSize},{color},{reason}");
                        }
                    }

                    MessageBox.Show(
                        $"Successfully exported {_filteredFindings.Count} findings to:\n\n{dialog.FileName}",
                        "Export Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    StatusTextBlock.Text = $"Exported {_filteredFindings.Count} findings to CSV";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting file: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Escape CSV field values with proper quoting
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return $"\"{field}\"";
        }

        /// <summary>
        /// Open Impressum (About) dialog
        /// </summary>
        private void ImpressionButton_Click(object sender, RoutedEventArgs e)
        {
            var impressumWindow = new ImpressumWindow();
            impressumWindow.Owner = this;
            impressumWindow.ShowDialog();
        }
    }

    public class AnalysisResult
    {
        public string File { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int TotalFindings { get; set; }
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public string Error { get; set; } = string.Empty;
    }

    public class Finding
    {
        public int? Page { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string FontName { get; set; } = string.Empty;
        public double? FontSize { get; set; }
        public string ColorRgb { get; set; } = string.Empty;
        public List<string> HiddenReasons { get; set; } = new List<string>();

        // UI helper not serialized
        public string HiddenReasonsJoined { get; set; } = string.Empty;
        public string Sheet { get; set; } = string.Empty;
        public string Cell { get; set; } = string.Empty;
    }
}
