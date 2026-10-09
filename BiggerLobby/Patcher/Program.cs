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
    BiggerLobbySetup.Setup.Run(args);
}
catch (IOException e)
{
    BiggerLobbySetup.Setup.Fail($"Could not write the game file. Make sure MIMESIS is closed, then try again.\n({e.Message})");
}
catch (Exception e)
{
    BiggerLobbySetup.Setup.Fail($"Unexpected error: {e}");
}
Console.WriteLine();
Console.WriteLine("Press any key to close.");
try { Console.ReadKey(true); } catch (InvalidOperationException) { }
