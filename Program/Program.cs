using System.Diagnostics;
using System.Management;

namespace Program;

static class Program
{
    static void Main()
    {
        while (true)
        {
            Console.WriteLine("\nОберіть дію:");
            Console.WriteLine("1. Вивести всі процеси");
            Console.WriteLine("2. Відобразити дерево процесів від заданого PID");
            Console.WriteLine("3. Зупинити дерево процесів від заданого PID");
            Console.WriteLine("4. Вихід\n");
            Console.Write("Введіть номер дії: ");
            
            string? choice = Console.ReadLine()?.Trim();
            
            switch (choice)
            {
                case "1":
                    Console.WriteLine();
                    DisplayAllProcesses();
                    break;
                
                case "2":
                    Console.Write("Введіть початковий PID: ");
                    int startPidForTree = Convert.ToInt32(Console.ReadLine());
                    DisplayProcessTree(startPidForTree);
                    break;
                
                case "3":
                    Console.Write("Введіть початковий PID: ");
                    int startPidForKill = Convert.ToInt32(Console.ReadLine());
                    KillProcessTree(startPidForKill);
                    break;
                
                case "4":
                    return;
                
                default:
                    Console.WriteLine("Невірний вибір!");
                    break;
            }
        }
    }
    
    static void DisplayAllProcesses()
    {
        ManagementObjectSearcher searcher = new ("SELECT ProcessId, ParentProcessId, Name FROM Win32_Process");
        
        foreach (ManagementBaseObject? o in searcher.Get())
        {
            var obj = (ManagementObject)o;
            Console.WriteLine("Process ID: {0}, Parent Process ID: {1}, Name: {2}", obj["ProcessId"], obj["ParentProcessId"], obj["Name"]);
        }
    }
    
    static void DisplayProcessTree(int pid)
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ProcessId = {pid}");
        
        if (searcher.Get().Count == 0)
        {
            Console.WriteLine($"Процес з PID {pid} не знайдено.");
            return;
        }

        Dictionary<int, List<int>> processTree = BuildProcessTree();
        
        Console.WriteLine($"\nВиведення дерева процесів для PID {pid}:");
        
        if (!HasChildProcesses(pid, processTree))
        {
            Console.WriteLine($"Процес з PID {pid} не має дочірніх процесів.");
            return;
        }

        DisplayProcessTreeRecursive(pid, processTree, 0);
    }

    static void DisplayProcessTreeRecursive(int pid, Dictionary<int, List<int>> processTree, int indent)
    {
        if (!processTree.ContainsKey(pid))
        {
            Console.WriteLine(new string(' ', indent * 2) + $"Процес з PID {pid} не має дочірніх процесів.");
            return;
        }

        Console.WriteLine(new string(' ', indent * 2) + $"Process ID: {pid}");

        bool hasChildren = false;
        
        foreach (int childPid in processTree[pid])
        {
            hasChildren = true;
            DisplayProcessTreeRecursive(childPid, processTree, indent + 1);
        }

        if (!hasChildren)
        {
            Console.WriteLine(new string(' ', indent * 2) + $"Процес з PID {pid} не має дочірніх процесів.");
        }
    }
    
    static Dictionary<int, List<int>> BuildProcessTree()
    {
        Dictionary<int, List<int>> processTree = new Dictionary<int, List<int>>();

        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process");
        
        foreach (ManagementObject obj in searcher.Get())
        {
            int processId = Convert.ToInt32(obj["ProcessId"]);
            int parentProcessId = Convert.ToInt32(obj["ParentProcessId"]);

            if (!processTree.ContainsKey(parentProcessId)) processTree[parentProcessId] = new List<int>();

            processTree[parentProcessId].Add(processId);
        }

        return processTree;
    }
    
    static bool HasChildProcesses(int pid, Dictionary<int, List<int>> processTree)
    {
        return processTree.ContainsKey(pid) && processTree[pid].Count > 0;
    }
    
    static void KillProcessTree(int pid)
    {
        Dictionary<int, List<int>> processTree = BuildProcessTree();
        KillProcessTreeRecursive(pid, processTree);
    }

    static void KillProcessTreeRecursive(int pid, Dictionary<int, List<int>> processTree)
    {
        if (processTree.ContainsKey(pid))
        {
            foreach (int childPid in processTree[pid])
            {
                KillProcessTreeRecursive(childPid, processTree);
            }
        }
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/F /PID " + pid,
                RedirectStandardOutput = true,
                RedirectStandardError = true,  
                UseShellExecute = false,       
                CreateNoWindow = true           
            };

            using (var process = Process.Start(startInfo))
            {
                process?.WaitForExit();
            }

            Console.WriteLine($"Процес з PID {pid} зупинено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка при зупинці процесу {pid}: {ex.Message}");
        }
    }
}