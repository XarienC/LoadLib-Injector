using Injector.Injection;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using System.Windows;

namespace Injector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Process/Refresh

        // Populates the ComboBox with the current running processes.
        private void RefreshProcessList()
        {
            ProcList.Items.Clear(); // Clears existing items in the ComboBox

            var processes = Process.GetProcesses()
        .Where(p =>
        {
            try
            {
                // Filter criteria:
                // 1. Process has a main window title (indicating it is user-facing)
                // 2. Process executable path is not null and located in a user directory
                string filePath = p.MainModule?.FileName;
                bool isUserApplication = !string.IsNullOrEmpty(filePath) &&
                                         (filePath.StartsWith(@"C:\Program Files") || filePath.StartsWith(@"C:\Users"));
                return !string.IsNullOrEmpty(p.MainWindowTitle) || isUserApplication;
            }
            catch
            {
                return false;
            }
        })
        .OrderBy(p => p.ProcessName); // Sort by process name

            foreach (var process in processes)
            {
                try
                {
                    // Get the architecture (x86 or x64) of the process
                    string architecture = GetProcessArchitecture(process);

                    // Create a display string with the process name, PID, and architecture
                    string displayText = $"{process.ProcessName} (PID: {process.Id}) [{architecture}]";

                    // Add the display string to the ComboBox
                    ProcList.Items.Add(displayText);
                }
                catch
                {
                    // Skip processes that throw exceptions (e.g., inaccessible processes)
                }
            }
        }

        // Determines the architecture of said process.
        private string GetProcessArchitecture(Process process)
        {
            try
            {
                // Query the ManagementObjectSearcher for process information.
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        // Check if the process has an executable path
                        var handle = obj["ExecutablePath"];
                        if (handle == null) continue;

                        // Return "x64" if the process is 64-bit, otherwise "x86"
                        if (Is64BitProcess(process)) return "x64";
                        return "x86";
                    }
                }
            }
            catch
            {
                // Returns "Unknown" if an error occurs
                return "Unknown";
            }

            // Defaults to "Unknown" if no architecture is determined
            return "Unknown";
        }

        // Checks if the specified process is a 64-bit process
        private bool Is64BitProcess(Process process)
        {
            // If the OS is 64-bit, check if the process is running under WOW64
            if (Environment.Is64BitOperatingSystem)
            {
                bool isWow64;
                if (NativeMethods.IsWow64Process(process.Handle, out isWow64))
                {
                    return !isWow64; // If not running under WOW64, it's a 64-bit process
                }
            }

            // If the OS is not 64-bit, all processes are 32-bit
            return !Environment.Is64BitOperatingSystem;
        }

        // Native method for determining if a process is running under WOW64 (32-bit on 64-bit OS)
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Winapi)]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool IsWow64Process(IntPtr processHandle, out bool isWow64);
        }

        // Displays detailed information about the selected process
        private void DisplayProcessDetails(string processName)
        {
            // Clear any existing text in the RichTextBox
            ProcDetailsTB.Document.Blocks.Clear();

            try
            {
                // Find the process by its name
                var process = Process.GetProcessesByName(processName)?.FirstOrDefault();
                if (process == null) // If the process is not found
                {
                    AppendTextToRichTextBox("Process not found.");
                    return;
                }

                // Build a string with details about the process
                StringBuilder details = new StringBuilder();
                details.AppendLine($"Process Name: {process.ProcessName}"); // Name of the process
                details.AppendLine($"PID: {process.Id}"); // Process ID
                details.AppendLine($"Start Time: {process.StartTime}"); // When the process started
                details.AppendLine($"Memory Usage: {process.WorkingSet64 / 1024 / 1024} MB"); // Memory usage in MB
                details.AppendLine($"Threads: {process.Threads.Count}"); // Number of threads
                details.AppendLine($"Priority: {process.BasePriority}"); // Process priority level

                // Add the details to the RichTextBox
                AppendTextToRichTextBox(details.ToString());
            }
            catch (Exception ex) // Handle cases where process details cannot be retrieved
            {
                AppendTextToRichTextBox($"Failed to retrieve details: {ex.Message}");
            }
        }

        // Helper method to append text to the RichTextBox
        private void AppendTextToRichTextBox(string text)
        {
            ProcDetailsTB.AppendText(text + Environment.NewLine); // Append the text with a new line
        }

        private void ProcList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProcList.SelectedItem != null) // Ensure an item is selected
            {
                // Extract the process name from the ComboBox item
                string selectedProcess = ProcList.SelectedItem.ToString().Split(' ')[0];

                // Display the details of the selected process in the RichTextBox
                DisplayProcessDetails(selectedProcess);
            }
        }

        private void RefreshListButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RefreshProcessList();
        }

        #endregion

        #region DLL Selection

        // Opens a file dialog to select a DLL file and updates the DLLPathTB with the selected path
        private void SelectDLLFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a DLL File",
                Filter = "DLL Files (*.dll)|*.dll", // Only allows DLL files to be selected.
                Multiselect = false // Allows only one file to be selected.
            };

            // Show the dialog and check if the user selected a file.
            if (openFileDialog.ShowDialog() == true)
            {
                // Sets the selected file path to the DLLPathTB textbox.
                DLLPathTB.Text = openFileDialog.FileName;

                // Display the details of the selected DLL in DLLDetailsTB
                DisplayDllDetails(openFileDialog.FileName);
            }
        }

        private void DisplayDllDetails(string dllPath)
        {
            DLLDetailsTB.Document.Blocks.Clear(); // Clear existing content

            try
            {
                // Create a FileInfo object to retrieve file details
                FileInfo dllFile = new FileInfo(dllPath);

                // Build a string with details about the DLL
                StringBuilder details = new StringBuilder();
                details.AppendLine($"File Name: {dllFile.Name}"); // DLL file name
                details.AppendLine($"File Path: {dllFile.FullName}"); // Full file path
                details.AppendLine($"File Size: {dllFile.Length / 1024.0:F2} KB"); // File size in KB
                details.AppendLine($"Created On: {dllFile.CreationTime}"); // File creation date
                details.AppendLine($"Last Modified: {dllFile.LastWriteTime}"); // Last modified date

                // Add the details to the DLLDetailsTB RichTextBox
                AppendTextToRichTextBox(DLLDetailsTB, details.ToString());
            }
            catch (Exception ex) // Handle cases where file details cannot be retrieved
            {
                AppendTextToRichTextBox(DLLDetailsTB, $"Failed to retrieve DLL details: {ex.Message}");
            }
        }

        // Helper method to append text to a RichTextBox
        private void AppendTextToRichTextBox(System.Windows.Controls.RichTextBox richTextBox, string text)
        {
            richTextBox.AppendText(text + Environment.NewLine); // Append the text with a new line
        }

        private void SelectDLLButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SelectDLLFile();
        }

        #endregion

        #region Injection
        private void InjectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Ensure a process and DLL are selected
            if (ProcList.SelectedItem == null || string.IsNullOrEmpty(DLLPathTB.Text))
            {
                MessageBox.Show("Please select a process and a DLL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Extracts the process name and the DLL path
            string selectedProcess = ProcList.SelectedItem.ToString().Split(' ')[0]; // Process name
            string dllPath = DLLPathTB.Text;

            // Performs injection
            bool success = LoadLibrary.Inject(selectedProcess, dllPath);
            if (success)
            {
                MessageBox.Show("DLL successfully injected!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("DLL injection failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion


    }
}