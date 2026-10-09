// MIMESIS More Players Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

// Usage: MorePlayersPatcher [path to Assembly-CSharp.dll]
// With no path, looks next to the exe, then in MIMESIS_Data\Managed below it.
// The limit is fixed at 10: the UI patches in BiggerLobbyRuntime are laid out for exactly 10 players.
var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
Console.Title = $"MIMESIS More Players Patcher v{version}";
Console.WriteLine($"MIMESIS More Players Patcher v{version}");
Console.WriteLine("by Mohamed Darwesh (@medovanx) - github.com/medovanx");
Console.WriteLine();
try
{
    Run(args);
}
catch (IOException e)
{
    Fail($"Could not write the game file. Make sure MIMESIS is closed, then try again.\n({e.Message})");
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
    var dll = args.Length > 0 ? args[0] : new[] {
        Path.Combine(here, "Assembly-CSharp.dll"),
        Path.Combine(here, "MIMESIS_Data", "Managed", "Assembly-CSharp.dll"),
    }.FirstOrDefault(File.Exists);
    if (dll == null)
    {
        Fail("Assembly-CSharp.dll not found.\nPut this exe in the MIMESIS game folder (next to MIMESIS.exe) and run it again.");
        return;
    }

    const int max = 10;
    var managed = Path.GetDirectoryName(dll);

    var bak = dll + ".bak";
    if (!File.Exists(bak)) File.Copy(dll, bak);

    var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(Path.GetDirectoryName(dll));
    // Patch the current DLL (not the backup) so other patches like PSController are kept.
    using var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(dll)), new ReaderParameters { AssemblyResolver = resolver });
    var module = asm.MainModule;

    // The limit lives in one static field, BiggerLobbyConfig.MaxPlayers, so re-running only changes its initializer.
    var config = module.GetType("BiggerLobbyConfig");
    if (config == null)
    {
        config = new TypeDefinition("", "BiggerLobbyConfig",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, module.TypeSystem.Object);
        config.Fields.Add(new FieldDefinition("MaxPlayers", FieldAttributes.Public | FieldAttributes.Static, module.TypeSystem.Int32));
        var cctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 4));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, config.Fields[0]));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        config.Methods.Add(cctor);
        module.Types.Add(config);
    }
    var maxField = config.Fields.First(f => f.Name == "MaxPlayers");
    config.Methods.First(m => m.Name == ".cctor").Body.Instructions[0].Operand = max;

    // Fixed-size UI / test helpers that only read the const; leave them at 4.
    string[] skip = { "UIPrefab_SurvivalResult", "VWorld" };
    static TypeDefinition Root(TypeDefinition t) { while (t.DeclaringType != null) t = t.DeclaringType; return t; }
    static bool IsLdcI4(Instruction x) => x.OpCode.Code is >= Code.Ldc_I4_M1 and <= Code.Ldc_I4;
    bool IsMax(Instruction x) => x.OpCode == OpCodes.Ldsfld && x.Operand is FieldReference f && f.Name == "MaxPlayers" && f.DeclaringType.Name == "BiggerLobbyConfig";

    int checks = 0, lobbies = 0;
    foreach (var type in module.GetTypes().Where(t => t != config && !skip.Contains(Root(t).Name)))
    foreach (var m in type.Methods.Where(m => m.HasBody))
    {
        var il = m.Body.GetILProcessor();
        var ins = m.Body.Instructions;
        for (int i = 0; i < ins.Count; i++)
        {
            var x = ins[i];
            if (x.OpCode == OpCodes.Ldfld && x.Operand is FieldReference f && f.Name == "C_MaxPlayerCount")
            {
                // obj.C_MaxPlayerCount -> pop obj; push MaxPlayers (keep x as first instr so branch targets stay valid)
                x.OpCode = OpCodes.Pop; x.Operand = null;
                il.InsertAfter(x, Instruction.Create(OpCodes.Ldsfld, maxField));
                checks++;
            }
            else if (x.OpCode == OpCodes.Pop && i > 0 && i + 1 < ins.Count
                && ins[i - 1].Operand is MemberReference prev && prev.Name.EndsWith("Consts")
                && (IsMax(ins[i + 1]) || IsLdcI4(ins[i + 1])))
            {
                // Already patched: by this version (ldsfld) or by the first release, which baked in a constant.
                ins[i + 1].OpCode = OpCodes.Ldsfld; ins[i + 1].Operand = maxField;
                checks++;
            }
            else if (x.OpCode == OpCodes.Call && x.Operand is MethodReference mr
                && mr.DeclaringType.FullName == "Steamworks.SteamMatchmaking" && mr.Name == "CreateLobby"
                && i > 0 && (IsLdcI4(ins[i - 1]) || IsMax(ins[i - 1])))
            {
                ins[i - 1].OpCode = OpCodes.Ldsfld; ins[i - 1].Operand = maxField;
                lobbies++;
            }
        }
    }

    if (checks == 0 || lobbies == 0)
    {
        Fail($"Nothing to patch (found {checks} player checks, {lobbies} lobby). This game version isn't supported. Your game was not changed.");
        return;
    }

    // Start the UI patches (BiggerLobbyRuntime.dll, via Harmony) from Hub.Awake.
    var awake = module.GetType("Hub").Methods.First(m => m.Name == "Awake" && !m.HasParameters);
    bool hooked = awake.Body.Instructions.Any(x => x.Operand is MethodReference h && h.DeclaringType.FullName == "BiggerLobby.Plugin");
    if (!hooked)
    {
        using var rt = AssemblyDefinition.ReadAssembly(Resource("BiggerLobbyRuntime.dll"), new ReaderParameters { AssemblyResolver = resolver });
        var init = module.ImportReference(rt.MainModule.GetType("BiggerLobby.Plugin").Methods.First(m => m.Name == "Init"));
        awake.Body.GetILProcessor().InsertBefore(awake.Body.Instructions[0], Instruction.Create(OpCodes.Call, init));
    }
    foreach (var name in new[] { "BiggerLobbyRuntime.dll", "0Harmony.dll" })
    {
        using var s = Resource(name);
        using var f = File.Create(Path.Combine(managed, name));
        s.CopyTo(f);
    }

    var tmp = dll + ".tmp";
    asm.Write(tmp);
    File.Copy(tmp, dll, true);
    File.Delete(tmp);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine();
    Console.WriteLine($"SUCCESS! Max players set to {max}.");
    Console.ResetColor();
    Console.WriteLine($"Patched {checks} player-limit checks and the Steam lobby size.");
    Console.WriteLine("Installed UI support for 10 players (lobby list, result screens).");
    Console.WriteLine($"Original file backed up to: {bak}");
}

static Stream Resource(string name) => System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
    ?? throw new InvalidOperationException("Missing embedded resource " + name);

static void Fail(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("FAILED: " + msg);
    Console.ResetColor();
}
