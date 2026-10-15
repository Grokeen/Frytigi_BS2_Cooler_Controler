using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Bs2Cooler.Windows;

public static class StartupTaskManager
{
    public static string UserSid => WindowsIdentity.GetCurrent().User!.Value;
    public static string TaskName => "BS2 Cooler-" + UserSid;
    public static string InstalledExecutable => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BS2 Cooler", "Bs2Cooler.exe");
    public static bool IsAdministrator => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public static bool IsEnabled()
    {
        dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")!)!;
        try
        {
            service.Connect();
            dynamic task = service.GetFolder(@"\").GetTask(TaskName);
            return (bool)task.Enabled && string.Equals((string)task.Definition.Actions.Item(1).Path, InstalledExecutable, StringComparison.OrdinalIgnoreCase)
                && IsCurrentUser((string)task.Definition.Principal.UserId);
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80070002)) { return false; }
        finally { Marshal.FinalReleaseComObject(service); }
    }
    private static bool IsCurrentUser(string identity)
    {
        if (identity == UserSid) return true;
        try { return ((SecurityIdentifier)new NTAccount(identity).Translate(typeof(SecurityIdentifier))).Value == UserSid; }
        catch (IdentityNotMappedException) { return false; }
    }

    public static void Run()
    {
        dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")!)!;
        try { service.Connect(); service.GetFolder(@"\").GetTask(TaskName).Run(null); }
        finally { Marshal.FinalReleaseComObject(service); }
    }
    public static void SetEnabled(bool enabled)
    {
        if (!IsAdministrator) throw new InvalidOperationException("자동 실행 설정을 바꾸려면 앱을 관리자 권한으로 실행해 주세요.");
        dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")!)!;
        try
        {
            service.Connect(); dynamic folder = service.GetFolder(@"\");
            if (!enabled)
            {
                try { folder.DeleteTask(TaskName, 0); }
                catch (COMException ex) when (ex.HResult == unchecked((int)0x80070002)) { }
                return;
            }
            if (!File.Exists(InstalledExecutable)) throw new FileNotFoundException("먼저 자동 실행용 앱 설치를 완료해 주세요.", InstalledExecutable);
            dynamic definition = service.NewTask(0);
            definition.RegistrationInfo.Author = WindowsIdentity.GetCurrent().Name;
            definition.RegistrationInfo.Description = "BS2 Cooler: current-user logon, tray background, CPU/GPU sensing.";
            definition.Principal.UserId = UserSid;
            definition.Principal.LogonType = 3; // Interactive token: never run a tray app in session zero.
            definition.Principal.RunLevel = 1; // CPU sensor access requires elevation.
            dynamic trigger = definition.Triggers.Create(9);
            trigger.UserId = UserSid; trigger.Delay = "PT10S";
            definition.Settings.Enabled = true;
            definition.Settings.StartWhenAvailable = true;
            definition.Settings.DisallowStartIfOnBatteries = false;
            definition.Settings.StopIfGoingOnBatteries = false;
            definition.Settings.ExecutionTimeLimit = "PT0S";
            definition.Settings.MultipleInstances = 2;
            dynamic action = definition.Actions.Create(0);
            action.Path = InstalledExecutable;
            action.Arguments = "--background --startup";
            action.WorkingDirectory = Path.GetDirectoryName(InstalledExecutable);
            folder.RegisterTaskDefinition(TaskName, definition, 6, UserSid, null, 3, null);
        }
        finally { Marshal.FinalReleaseComObject(service); }
    }
}
