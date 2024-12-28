using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Injector.Injection
{
    public static class LoadLibrary
    {
        // Import necessary Windows API functions
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint INFINITE = 0xFFFFFFFF;

        public static bool Inject(string processName, string dllPath)
        {
            try
            {
                // Get the target process
                var targetProcess = Process.GetProcessesByName(processName)?.FirstOrDefault();
                if (targetProcess == null)
                {
                    Console.WriteLine("Target process not found.");
                    return false;
                }

                // Open the process with all access
                IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
                if (processHandle == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to open target process.");
                    return false;
                }

                // Allocate memory in the target process for the DLL path
                IntPtr allocatedMemory = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)dllPath.Length + 1, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (allocatedMemory == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to allocate memory in target process.");
                    CloseHandle(processHandle);
                    return false;
                }

                // Write the DLL path into the allocated memory
                byte[] dllBytes = System.Text.Encoding.ASCII.GetBytes(dllPath);
                if (!WriteProcessMemory(processHandle, allocatedMemory, dllBytes, (uint)dllBytes.Length, out _))
                {
                    Console.WriteLine("Failed to write to process memory.");
                    CloseHandle(processHandle);
                    return false;
                }

                // Get the address of LoadLibraryA
                IntPtr loadLibraryAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddress == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to get LoadLibraryA address.");
                    CloseHandle(processHandle);
                    return false;
                }

                // Create a remote thread in the target process to call LoadLibraryA
                IntPtr remoteThread = CreateRemoteThread(processHandle, IntPtr.Zero, 0, loadLibraryAddress, allocatedMemory, 0, out _);
                if (remoteThread == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to create remote thread.");
                    CloseHandle(processHandle);
                    return false;
                }

                // Wait for the thread to finish
                WaitForSingleObject(remoteThread, INFINITE);

                // Clean up
                CloseHandle(remoteThread);
                CloseHandle(processHandle);

                Console.WriteLine("DLL successfully injected.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Injection failed: {ex.Message}");
                return false;
            }
        }
    }
}
