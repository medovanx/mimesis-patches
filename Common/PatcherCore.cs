// MIMESIS Patches - shared patcher logic for Harmony-based patches
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Compiled into the patcher exe of patches whose logic lives in a runtime DLL (see SpectatorCam, Minimap).
// Install: back up Assembly-CSharp.dll once, add "<Plugin>.Init()" to the start of Hub.Awake, and copy the
// embedded runtime DLL + 0Harmony.dll into MIMESIS_Data/Managed. Uninstall removes the hook and the runtime.
// Always patches the current DLL so other patches are kept.

using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MimesisPatches
{
    static class PatcherCore
    {
        /// <param name="plugin">Full name of the runtime's static class with an Init() method, e.g. "SpectatorCam.Plugin".</param>
        /// <param name="runtimeDll">Embedded resource / file name of the runtime, e.g. "SpectatorCamRuntime.dll".</param>
        public static void Main(string[] args, string title, string description, string plugin, string runtimeDll)
        {
            var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(3);
            Console.Title = $"{title} v{version}";
            Console.WriteLine($"{title} v{version} - {description}");
            Console.WriteLine("by Mohamed Darwesh (@medovanx) - github.com/medovanx");
            Console.WriteLine();
            try
            {
                Run(args, title, plugin, runtimeDll);
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
        }

        // Usage: <exe> [install|uninstall] [path to Assembly-CSharp.dll]
        public static void Run(string[] args, string title, string plugin, string runtimeDll)
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
            bool install = (args.Length > 0 ? args[0].ToLowerInvariant() : AskMode()) != "uninstall";

            var bak = dll + ".bak";
            if (!File.Exists(bak)) File.Copy(dll, bak);

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(managed);
            using var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(dll)), new ReaderParameters { AssemblyResolver = resolver });
            var module = asm.MainModule;

            var awake = module.GetType("Hub")?.Methods.FirstOrDefault(m => m.Name == "Awake" && !m.HasParameters);
            if (awake == null)
            {
                Fail("Hub.Awake not found. This game version isn't supported. Your game was not changed.");
                return;
            }
            var hook = awake.Body.Instructions.FirstOrDefault(x => x.Operand is MethodReference h && h.DeclaringType.FullName == plugin);
            bool changed = false;
            if (install && hook == null)
            {
                using var rt = AssemblyDefinition.ReadAssembly(Resource(runtimeDll), new ReaderParameters { AssemblyResolver = resolver });
                var init = module.ImportReference(rt.MainModule.GetType(plugin).Methods.First(m => m.Name == "Init"));
                awake.Body.GetILProcessor().InsertBefore(awake.Body.Instructions[0], Instruction.Create(OpCodes.Call, init));
                changed = true;
            }
            else if (!install && hook != null)
            {
                awake.Body.Instructions.Remove(hook);
                var reference = module.AssemblyReferences.FirstOrDefault(a => a.Name == Path.GetFileNameWithoutExtension(runtimeDll));
                if (reference != null) module.AssemblyReferences.Remove(reference);
                changed = true;
            }
            if (changed)
            {
                var tmp = dll + ".tmp";
                asm.Write(tmp);
                File.Copy(tmp, dll, true);
                File.Delete(tmp);
            }

            if (install)
            {
                foreach (var name in new[] { runtimeDll, "0Harmony.dll" })
                {
                    using var s = Resource(name);
                    using var f = File.Create(Path.Combine(managed, name));
                    s.CopyTo(f);
                }
                Success($"SUCCESS! {Short(title)} installed.");
                Console.WriteLine("To remove it, run this exe again and choose uninstall.");
            }
            else
            {
                var path = Path.Combine(managed, runtimeDll);
                if (File.Exists(path)) File.Delete(path);   // 0Harmony.dll stays: other patches may use it
                Success($"SUCCESS! {Short(title)} removed.");
            }
        }

        static string Short(string title) => title.Replace("MIMESIS ", "").Replace(" Patcher", "");

        static string AskMode()
        {
            while (true)
            {
                Console.Write("Install or uninstall? (i/u, Enter = install): ");
                var s = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(s) || s == "i" || s == "install") return "install";
                if (s == "u" || s == "uninstall") return "uninstall";
            }
        }

        /// <summary>Set by the all-in-one installer to read patch files it downloaded instead of embedded ones.</summary>
        public static Func<string, Stream> ResourceOverride;

        static Stream Resource(string name) => ResourceOverride?.Invoke(name) ?? System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Missing embedded resource " + name);

        public static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void Fail(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("FAILED: " + msg);
            Console.ResetColor();
        }
    }
}
