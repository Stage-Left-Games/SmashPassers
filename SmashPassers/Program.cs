using System;
using System.IO;

using SmashPassers;

using Jelly;
using Jelly.IO;

internal class Program
{
    public static bool UseSteamworks { get; private set; } = true;
    public static ulong LobbyToJoin { get; set; } = 0;

    internal static StreamWriter LogFile { get; private set; }

    private static TextWriter oldOut;
    private static TextWriter oldError;
    private static TextWriterWrapper _consoleOut;
    private static TextWriterWrapper _consoleError;

    public static string SaveDataPath => Path.Combine(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    private static void Main(string[] args)
    {
        LogFile = File.CreateText(Path.Combine(ProgramPath, "latest.log"));
        LogFile.AutoFlush = true;
        LogFile.NewLine = "\n";

        oldOut = Console.Out;
        oldError = Console.Error;

        _consoleOut = new(oldOut);
        _consoleOut.OnWrite += Log;
        _consoleOut.OnWriteFormatted += LogFormatted;
        _consoleOut.OnWriteLine += LogLine;
        _consoleOut.OnWriteLineFormatted += LogLineFormatted;

        _consoleError = new(oldError);
        _consoleError.OnWrite += Log;
        _consoleError.OnWriteFormatted += LogFormatted;
        _consoleError.OnWriteLine += LogLine;
        _consoleError.OnWriteLineFormatted += LogLineFormatted;

        Console.SetOut(_consoleOut);
        Console.SetError(_consoleError);

        using var game = new Main();

        try
        {
            game.Run();
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private static void Log(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
        {
            int i = callback.Index ?? 0, c = callback.Count ?? buffer.Length;
            LogFile.Write(buffer, i, c);
        }
        else
        {
            LogFile.Write(callback.Value);
        }
    }

    private static void LogFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.Write(callback.Format, callback.Arg);
    }

    private static void LogLine(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
        {
            int i = callback.Index ?? 0, c = callback.Count ?? buffer.Length;
            LogFile.WriteLine(buffer, i, c);
        }
        else
        {
            LogFile.WriteLine(callback.Value);
        }
    }

    private static void LogLineFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.WriteLine(callback.Format, callback.Arg);
    }
}
