using System;
using System.Collections.Generic;
using System.Linq;
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

            default:
                throw new CommandParsingException($"Unknown command `{words[0]}`");
        }
    }
}

internal sealed class ShuffleCommand : Command { }
internal sealed class LoadScriptsCommand : Command { }
internal sealed class ListScriptsCommand : Command { }
internal sealed class SelectScriptCommand : Command 
{
    public required string Script { get; set; }
}
internal sealed class PlayScriptCommand : Command { }
internal sealed class StopScriptCommand : Command { }