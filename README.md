# C# DLL Injector (WPF, .NET 8.0)

This project is a **C# DLL Injector** built using **Windows Presentation Foundation (WPF)** and **.NET 8.0**, designed with a modern, clean, and intuitive Windows 11-style user interface. The application provides a robust and efficient way to inject DLLs into selected processes, making it a valuable tool for developers, testers, and researchers working in application testing, debugging, or process interaction.

## Example
![DLL Injector UI](https://i.imgur.com/3AAKg2D.png)

---

## Key Features

### 1. Process Selection
- Displays a filtered list of running processes on the user's system, focusing only on user-facing applications like games, Spotify, Discord, etc.
- Excludes system services, drivers, and other unnecessary background processes for a cleaner list.
- Processes are displayed with:
  - **Name**
  - **PID (Process ID)**
  - **Architecture** (x86 or x64)

### 2. DLL Selection
- Allows users to browse and select a DLL file for injection.
- Displays detailed information about the selected DLL, including:
  - File name
  - Full file path
  - File size
  - Creation date
  - Last modified date

### 3. Process Details
- Shows detailed information about the selected process in a dedicated section, including:
  - Process name
  - PID
  - Memory usage
  - Number of threads
  - Priority
  - File path to the executable

### 4. DLL Injection
- Implements the **LoadLibrary injection method**:
  - Allocates memory in the target process.
  - Writes the DLL path into the process memory.
  - Creates a remote thread to execute the DLL using `LoadLibraryA`.
- Supports both x86 and x64 architectures.

### 5. User-Friendly Interface
- Designed with a modern, Windows 11-inspired UI for simplicity and accessibility.
- Features a clean layout with interactive ComboBox, buttons, and rich text boxes for process and DLL details.

---

## How It Works

1. **Refresh Process List**:
   - Click the **Refresh** button to populate the list of running processes. Only user-facing applications are displayed.

2. **Select a Process**:
   - Choose a process from the ComboBox. The application will display detailed information about the selected process.

3. **Select a DLL**:
   - Click the **Select DLL** button to browse and select the DLL file you wish to inject. The selected DLL's details are displayed.

4. **Inject the DLL**:
   - Click the **Inject** button to perform the injection. The application uses the LoadLibrary method to load the DLL into the selected process.

---

## Usage Notes

- This application is designed for **educational and testing purposes** in a controlled environment. 
- Ensure you have appropriate permissions and comply with legal and ethical guidelines before using this tool.

---

## Technology Stack

- **Language**: C#
- **Framework**: WPF (Windows Presentation Foundation)
- **Runtime**: .NET 8.0
- **UI Style**: Windows 11-inspired design

---

## Disclaimer

This project is intended for **educational purposes only**. Unauthorized injection of DLLs into processes you do not own or have permission to modify is illegal and unethical. Use this tool responsibly and only in environments where you have explicit authorization.

---

Feel free to contribute or provide feedback to improve this project! 🚀
