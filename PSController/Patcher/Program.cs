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
    PSControllerSetup.Setup.Run(args);
}
catch (IOException e)
{
    PSControllerSetup.Setup.Fail($"Could not write the game files. Make sure MIMESIS is closed, then try again.\n({e.Message})");
}
catch (Exception e)
{
    PSControllerSetup.Setup.Fail($"Unexpected error: {e}");
}
Console.WriteLine();
Console.WriteLine("Press any key to close.");
try { Console.ReadKey(true); } catch (InvalidOperationException) { }
