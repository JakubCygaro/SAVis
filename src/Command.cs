using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SAVis;

[Serializable]
public class CommandParsingException : Exception
{
    public CommandParsingException() { }
    public CommandParsingException(string message) : base(message) { }
    public CommandParsingException(string message, Exception inner) : base(message, inner) { }

}
internal abstract class Command
{
    public abstract string FullName { get; }
    public abstract IEnumerable<string> Aliases { get; }

    public static Command ParseFromString(string input)
    {
        input.Trim();
        var words = input.Split(' ');
        if (words.Length == 0)
            throw new CommandParsingException("No command to parse");
        switch (words[0])
        {
            case "sh" or "shuffle":
                return new ShuffleCommand();

            case "ld" or "load":
                return new LoadScriptsCommand();

            case "ls" or "list":
                return new ListScriptsCommand();

            case "slc" or "select":
                var script = words[1..] ??
                    throw new CommandParsingException("Too few arguments provided, name of the script is required");
                var name = string.Join(' ', script);
                return new SelectScriptCommand()
                {
                    Script = name,
                };

            case "pl" or "play":
                return new PlayScriptCommand();

            case "st" or "stop":
                return new StopScriptCommand();

            case "cmd" or "commands":
                return new CommandsCommand();

            case "reld" or "reload":
                return new ReloadScripsCommand();

            case "order" or "ord":
                return new OrderCommand();

            case "elements" or "elem":
                return new ChangeElemCountCommand()
                {
                    Amount = int.Parse(words[1])
                };

            default:
                throw new CommandParsingException($"Unknown command `{words[0]}`");
        }
    }

    internal static Dictionary<string, IEnumerable<string>> GetCommandNamesWithAliases()
    {
        var assm = Assembly.GetExecutingAssembly();
        var ret = assm.GetTypes()
            .Where(static t => 
                t.IsClass &&
                !t.IsAbstract &&
                t.BaseType == typeof(Command)
            )
            .Select(cmdType =>
            {
                var cmdInstance = (Command)assm.CreateInstance(cmdType.FullName!)!;
                return cmdInstance;
            })
            .ToDictionary( static i => i.FullName, static i => i.Aliases);
        return ret;
    }
}

internal sealed class ShuffleCommand : Command 
{
    public override string FullName => "shuffle";
    public override IEnumerable<string> Aliases => ["sh"];
}
internal sealed class LoadScriptsCommand : Command 
{
    public override string FullName => "load";
    public override IEnumerable<string> Aliases => ["ld"];
}
internal sealed class ListScriptsCommand : Command 
{
    public override string FullName => "list";
    public override IEnumerable<string> Aliases => ["ls"];
}
internal sealed class SelectScriptCommand : Command 
{
    public override string FullName => "select";
    public override IEnumerable<string> Aliases => ["slc"];
    public required string Script { get; set; }
}
internal sealed class PlayScriptCommand : Command 
{
    public override string FullName => "play";
    public override IEnumerable<string> Aliases => ["pl"];
}
internal sealed class StopScriptCommand : Command 
{
    public override string FullName => "stop";
    public override IEnumerable<string> Aliases => ["st"];
}

internal sealed class CommandsCommand : Command
{
    public override string FullName => "commands";
    public override IEnumerable<string> Aliases => ["cmd"]; 
}

internal sealed class ReloadScripsCommand : Command
{
    public override string FullName => "reload";
    public override IEnumerable<string> Aliases => ["reld"];
}

internal sealed class OrderCommand : Command
{
    public override string FullName => "order";
    public override IEnumerable<string> Aliases => ["ord"];
}

internal sealed class ChangeElemCountCommand : Command
{
    public override string FullName => "elements";
    public override IEnumerable<string> Aliases => ["elem"];

    public required int Amount { get; init; }
}