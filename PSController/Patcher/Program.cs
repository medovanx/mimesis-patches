// MIMESIS PSController Patcher - PlayStation button icons
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Usage: PSControllerPatcher [install|uninstall] [path to Assembly-CSharp.dll]
// With no path, looks next to the exe, then in MIMESIS_Data\Managed below it.
var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
Console.Title = $"MIMESIS PSController Patcher v{version}";
Console.WriteLine($"MIMESIS PSController Patcher - PlayStation button icons v{version}");
Console.WriteLine("by Mohamed Darwesh (@medovanx) - github.com/medovanx");
Console.WriteLine();
try
{
    Run(args);
}
catch (IOException e)
{
    Fail($"Could not write the game files. Make sure MIMESIS is closed, then try again.\n({e.Message})");
}
catch (Exception e)
{
    Fail($"Unexpected error: {e}");
}
Console.WriteLine();
Console.WriteLine("Press any key to close.");
try { Console.ReadKey(true); } catch (InvalidOperationException) { }

static void Run(string[] args)
{
    var here = AppContext.BaseDirectory;
    var dll = args.Length > 1 ? args[1] : new[] {
        Path.Combine(here, "Assembly-CSharp.dll"),
        Path.Combine(here, "MIMESIS_Data", "Managed", "Assembly-CSharp.dll"),
    }.FirstOrDefault(File.Exists);
    if (dll == null)
    {
        Fail("Assembly-CSharp.dll not found.\nPut this exe in the MIMESIS game folder (next to MIMESIS.exe) and run it again.");
        return;
    }
    var managed = Path.GetDirectoryName(dll);
    var iconsDir = Path.Combine(Path.GetDirectoryName(managed), "PSIcons");
    var runtimeDll = Path.Combine(managed, "PSControllerRuntime.dll");

    var mode = args.Length > 0 ? args[0].ToLowerInvariant() : AskMode();
    bool install = mode != "uninstall";

    var bak = dll + ".bak";
    if (!File.Exists(bak)) File.Copy(dll, bak);

    // Patch the current DLL (not the backup) so other patches like BiggerLobby are kept.
    var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(managed);
    var bytes = File.ReadAllBytes(dll);
    using var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(bytes), new ReaderParameters { AssemblyResolver = resolver });
    var module = asm.MainModule;

    var method = module.GetType("KeyImageData")?.Methods
        .FirstOrDefault(m => m.Name == "GetKeyImage" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String");
    if (method == null)
    {
        Fail("KeyImageData.GetKeyImage not found. This game version isn't supported. Your game was not changed.");
        return;
    }

    var ins = method.Body.Instructions;
    bool patched = ins.Count > 1 && ins[1].Operand is MethodReference r && r.DeclaringType.FullName == "PSController.PSIcons";

    using var rt = AssemblyDefinition.ReadAssembly(Resource("PSControllerRuntime.dll"), new ReaderParameters { AssemblyResolver = resolver });
    var padsCurrent = rt.MainModule.GetType("PSController.Pads").Methods.First(m => m.Name == "get_Current");
    int redirected = 0;

    if (install && !patched)
    {
        // Prepend: var s = PSIcons.Get(keyName); if (s != null) return s;
        var get = module.ImportReference(rt.MainModule.GetType("PSController.PSIcons").Methods.First(m => m.Name == "Get"));
        var il = method.Body.GetILProcessor();
        var first = ins[0];
        var pop = il.Create(OpCodes.Pop);
        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
        il.InsertBefore(first, il.Create(OpCodes.Call, get));
        il.InsertBefore(first, il.Create(OpCodes.Dup));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse, pop));
        il.InsertBefore(first, il.Create(OpCodes.Ret));
        il.InsertBefore(first, pop);
    }
    else if (!install && patched)
    {
        for (int i = 0; i < 6; i++) ins.RemoveAt(0);
    }

    // Gamepad.current <-> Pads.Current everywhere, so the DS4Windows Xbox pad wins over the raw PS pad.
    MethodReference gamepadCurrent = null;
    foreach (var m in module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
    foreach (var x in m.Body.Instructions)
    {
        if (x.OpCode != OpCodes.Call || x.Operand is not MethodReference mr || mr.Name != "get_current" && mr.Name != "get_Current") continue;
        if (install && mr.DeclaringType.FullName == "UnityEngine.InputSystem.Gamepad")
        {
            x.Operand = module.ImportReference(padsCurrent);
            redirected++;
        }
        else if (!install && mr.DeclaringType.FullName == "PSController.Pads")
        {
            gamepadCurrent ??= module.ImportReference(resolver.Resolve(new AssemblyNameReference("Unity.InputSystem", null))
                .MainModule.GetType("UnityEngine.InputSystem.Gamepad").Methods.First(g => g.Name == "get_current"));
            x.Operand = gamepadCurrent;
            redirected++;
        }
    }

    if (!install)
    {
        var reference = module.AssemblyReferences.FirstOrDefault(a => a.Name == "PSControllerRuntime");
        if (reference != null) module.AssemblyReferences.Remove(reference);
    }
    if ((install && !patched) || (!install && patched) || redirected > 0) Save(asm, dll);

    if (install)
    {
        using (var s = Resource("PSControllerRuntime.dll")) using (var f = File.Create(runtimeDll)) s.CopyTo(f);
        Directory.CreateDirectory(iconsDir);
        int n = 0;
        foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("icons/")))
        {
            using var s = Resource(name);
            using var f = File.Create(Path.Combine(iconsDir, name.Substring("icons/".Length)));
            s.CopyTo(f);
            n++;
        }
        Success("SUCCESS! PlayStation button icons installed.");
        Console.WriteLine($"Installed {n} icons to: {iconsDir}");
        Console.WriteLine($"Controller fix: {redirected} gamepad lookups now prefer the Xbox (XInput) pad.");
        Console.WriteLine("To remove them, run this exe again and choose uninstall.");
    }
    else
    {
        if (File.Exists(runtimeDll)) File.Delete(runtimeDll);
        if (Directory.Exists(iconsDir)) Directory.Delete(iconsDir, true);
        Success("SUCCESS! PlayStation button icons removed. The game uses Xbox icons again.");
    }
}

static string AskMode()
{
    while (true)
    {
        Console.Write("Install or uninstall PlayStation icons? (i/u, Enter = install): ");
        var s = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s) || s == "i" || s == "install") return "install";
        if (s == "u" || s == "uninstall") return "uninstall";
    }
}

static Stream Resource(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
    ?? throw new InvalidOperationException("Missing embedded resource " + name);

static void Save(AssemblyDefinition asm, string dll)
{
    var tmp = dll + ".tmp";
    asm.Write(tmp);
    File.Copy(tmp, dll, true);
    File.Delete(tmp);
}

static void Success(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(msg);
    Console.ResetColor();
}

static void Fail(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("FAILED: " + msg);
    Console.ResetColor();
}
