using SAVis.API;
using Raylib_CsLo;
using System.Reflection;
using Raylib_CsLo.InternalHelpers;
using System.Globalization;
using Microsoft.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace SAVis;
internal class Program
{
    static SortingContext s_sortingContext;
    static ElementArray s_elementArray;

    static int s_elemCount = 50;
    static int s_width = 800;
    static int s_height = 600;
    const int c_fps = 60;
    const int c_minWidth = 800;
    const int c_minHeight = 600;
    const int c_drawFrames = 1;
    const int _textSize = 24;
    static int _commandHistorySize = 0;
    static int s_commandHistoryPtr = -1;
    static int s_maxTextLength = 0;

    static Font s_guiFont;
    static int s_guiTextSize = 0;
    static int s_guiTextSpacing = 0;

    static int s_commandHistoryOffset = 0;

    static AudioStream s_audioStream;
    static Rectangle _cmdHistBox => new Rectangle()
    {
        width = s_width, //(_width / 40) * 39,
        height = (s_height / 4) - _textSize + 2,
        x = 0,
        y = 0
    };

    static bool s_drawOverlay = false;

    static Dictionary<string, ISorter> s_sorters = new();

    static ISorter s_currentSorter;
    static IEnumerator<bool>? s_currentSorterEnumm;
    static Command? s_currentCommand;

    static string s_scriptsDirectoryLocation = "";
    static string s_inputText = "";
    static string _inputHistory = "";
    static LinkedList<string> s_commandHistory = new();
    static Dictionary<string, IEnumerable<string>> s_commands;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void AudioInputCallbackDelegate(void* buffer, uint frames);

    const int c_maxSamples = 512;
    const int c_maxSamplesPerUpdate = 4096;
    static float s_freq = 440.0f;
    static float s_audioFreq = 440.0f;
    static float s_oldFreq = 1.0f;
    static float s_sineIdx = 0.0f;
    static short[] s_audioData = [sizeof(short) * c_maxSamples];
    static short[] s_writeBuf = [sizeof(short) * c_maxSamplesPerUpdate];
    static unsafe void AudioInputCallback(void *buffer, uint frames)
    {
        s_audioFreq = s_audioFreq + (s_audioFreq - s_audioFreq) * 0.95f;

        float incr = s_audioFreq / 44100.0f;
        short* d = (short*)buffer;

        for (uint i = 0; i < frames; i++)
        {
            d[i] = (short)(32000.0f * Math.Sin(2 * Math.PI * s_sineIdx));
            s_sineIdx += incr;
            if (s_sineIdx > 1.0f) s_sineIdx -= 1.0f;
        }
    }

    static Program()
    {
        s_elementArray = new(s_elemCount);
        s_sortingContext = new(s_elementArray);
        s_currentSorter = ISorter.Default;
        s_sorters[s_currentSorter.Name] = s_currentSorter;
        s_commands = Command.GetCommandNamesWithAliases();

        var ci = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentCulture = ci;
        Thread.CurrentThread.CurrentUICulture = ci;
    }
    static void Main(string[] args)
    {
        SetupRaylib();
        SetupEnvironment();
        CalculateCommandHistorySize();
        MeasureMaxTextLenght();
        while(!Raylib.WindowShouldClose())
        {
            Update();
            for(int i = 0; i < c_drawFrames; i++)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Raylib.BLACK);
                Draw();
                if (s_drawOverlay)
                    DrawOverlay();
                Raylib.EndDrawing();
            }
        }
        Closing();
    }
    static void Closing()
    {
        Raylib.UnloadAudioStream(s_audioStream);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
    static void SetupRaylib()
    {
        Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.InitWindow(s_width, s_height, "SAVis");
        Raylib.SetWindowMinSize(c_minWidth, c_minHeight);
        Raylib.SetTargetFPS(c_fps);
        RayGui.GuiEnable();
        RayGui.GuiSetStyle((int)GuiControl.DEFAULT, (int)GuiControlProperty.TEXT_COLOR_NORMAL, 0xffffff);
        RayGui.GuiSetStyle((int)GuiControl.DEFAULT, (int)GuiControlProperty.TEXT_COLOR_FOCUSED, 0xffffff);
        s_guiFont = RayGui.GuiGetFont();
        s_guiTextSize = Helpers.GuiGetStyle(GuiControl.DEFAULT, GuiDefaultProperty.TEXT_SIZE);
        s_guiTextSpacing = Helpers.GuiGetStyle(GuiControl.TEXTBOX, GuiDefaultProperty.TEXT_SPACING);
#if DEBUG
        Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_ALL);
#endif
        //RayGui.GuiLoadStyleDefault();
        //Raylib.InitAudioDevice();
        //Raylib.SetAudioStreamBufferSizeDefault(c_maxSamples);
        //s_audioStream = Raylib.LoadAudioStream(44100, 16, 1);
        //unsafe
        //{
        //    var a = new AudioInputCallbackDelegate(AudioInputCallback);
        //    delegate* unmanaged[Cdecl]<void*, uint, void> d = 
        //        (delegate* unmanaged[Cdecl] <void*, uint, void>)Marshal.GetFunctionPointerForDelegate(a);
        //    Raylib.SetAudioStreamCallback(s_audioStream, d);
        //    Raylib.PlayAudioStream(s_audioStream);
        //}
    }
    static void SetupEnvironment()
    {
        var loc = Assembly.GetEntryAssembly()!.Location;
        loc = Directory.GetParent(loc)!.FullName;
        s_scriptsDirectoryLocation = $"{loc}/scripts" ;
        s_scriptsDirectoryLocation = Path.GetFullPath(s_scriptsDirectoryLocation);
        Directory.CreateDirectory(s_scriptsDirectoryLocation);
    }
    static void Update()
    {
        if (Raylib.IsWindowResized())
        {
            ResizeWindow();
        }

        ProcessInput();
        if(s_currentCommand is not null)
            ExcecuteCurrentCommand();
        s_elementArray.WhiteAll();
        
        if(s_currentSorterEnumm is not null)
        {
            if (s_currentSorterEnumm.Current)
                s_currentSorterEnumm = null;
            try
            {
                s_currentSorterEnumm?.MoveNext();
            }
            catch (Exception e)
            {
                AddCommandHistory("An error has occured while playing the script");
                AddCommandHistory($"Error message: `{e.Message}`");
                Console.WriteLine(e);
                s_currentSorterEnumm = null;
            }
        }

    }

    private static void ResizeWindow()
    {
        var handle = Raylib.GetCurrentMonitor();
        var (monW, monH) = (Raylib.GetMonitorWidth(handle), Raylib.GetMonitorHeight(handle));

        var (newW, newH) = (
            int.Clamp(Raylib.GetScreenWidth(), c_minWidth, monW),
            int.Clamp(Raylib.GetScreenHeight(), c_minHeight, monH)
            );

        s_width = newW;
        s_height = newH;
        CalculateCommandHistorySize();
        MeasureMaxTextLenght();
        UpdateCommandHistory();
    }

    static void Draw()
    {
        var wRatio = s_width / (float)s_elemCount;
        var hRatio = s_height / (float)s_elemCount;
        int index = 0;
        foreach( (uint, ElementColor) elem in s_elementArray )
        {
            var color = elem.Item2 switch
            {
                ElementColor.White => Raylib.WHITE,
                ElementColor.Red => Raylib.RED,
                ElementColor.Green => Raylib.GREEN,
                _ => Raylib.WHITE
            };

            Raylib.DrawRectangleRec(
                new() 
                { 
                    x = index * wRatio, 
                    y = s_height - hRatio * elem.Item1,
                    width = wRatio,
                    height = hRatio * elem.Item1
                },
                color) ;

            index++;
        }
    }

    static void DrawOverlay()
    {
        Raylib.DrawRectangleRec(new Rectangle()
        {
            x = 0,
            y = 0,
            width = s_width,
            height = s_height / 4,
        },
        new Color(0, 0, 0, 200));

        RayGui.GuiTextBoxMulti(_cmdHistBox, _inputHistory, _textSize, false);

        RayGui.GuiTextBox(
            new Rectangle()
            {
                width = s_width,
                height = _textSize + 2,
                x = 0,
                y = (s_height / 4) - _textSize + 2,
            }, s_inputText, _textSize, true);

    }

    static void ProcessInput()
    {
        if(Raylib.IsKeyReleased(KeyboardKey.KEY_GRAVE))
        {
            s_drawOverlay = !s_drawOverlay;
        }
        if (s_drawOverlay)
        {
            ReadInputText();
            int mouseWheel;
            if((mouseWheel = (int)Raylib.GetMouseWheelMove()) != 0)
            {
                s_commandHistoryOffset = int.Clamp(
                    (int)(s_commandHistoryOffset + mouseWheel), 
                    0, 
                    s_commandHistory.Count);
                UpdateCommandHistory();
            }
        }
    }

    static void ReadInputText()
    {
        int key;
        while((key = Raylib.GetCharPressed()) > 0)
        {
            if ((char)key != '~')
                s_inputText += (char)key;
        }
        if(Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE))
        {
            if(s_inputText.Length > 0)
                s_inputText = s_inputText[..^1];
            s_commandHistoryPtr = -1;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
        {
            try
            {
                s_currentCommand = Command.ParseFromString(s_inputText);
                AddCommandHistory(s_inputText);
            }
            catch(CommandParsingException cpm)
            {
                AddCommandHistory(cpm.Message);
            }
            catch(Exception e)
            {
                AddCommandHistory(e.Message);
            }
            finally
            {
                s_inputText = "";
                s_commandHistoryPtr = -1;
                s_commandHistoryOffset = 0;
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
        {
            s_commandHistoryPtr++;
            var elem = s_commandHistory.ElementAtOrDefault(s_commandHistoryPtr);
            if (elem is not null)
            {
                s_inputText = elem;
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
        {
            s_commandHistoryPtr--;
            s_commandHistoryPtr = int.Clamp(s_commandHistoryPtr, -1, s_commandHistory.Count);
            var elem = s_commandHistory.ElementAtOrDefault(s_commandHistoryPtr);
            if (elem is not null)
            {
                s_inputText = elem;
            }
        }
    }

    static void ExcecuteCurrentCommand()
    {
        switch(s_currentCommand)
        {
            case ShuffleCommand:
                s_elementArray.Shuffle();
            break;

            case LoadScriptsCommand:
                LoadScripts();
            break;

            case ListScriptsCommand:
                AddCommandHistory("Currently loaded sorter scripts:");
                int i = 1;
                foreach(var (name, sorter) in s_sorters)
                {
                    AddCommandHistory($"\t {i++}. {name} by {sorter.Author}");
                }
            break;

            case SelectScriptCommand select:
                if (s_sorters.GetValueOrDefault(select.Script) is ISorter selection)
                {
                    s_currentSorter = selection;
                    AddCommandHistory($"Selected `{selection.Name}`");
                }
                else
                {
                    AddCommandHistory($"No such script `{select.Script}`");
                }
            break;

            case PlayScriptCommand:
                try
                {
                    s_currentSorterEnumm = s_currentSorter.Update(s_sortingContext);
                    AddCommandHistory($"Now playing `{s_currentSorter.Name}`");
                }
                catch (Exception e)
                {
                    AddCommandHistory("An error has occured while attempting to play the script");
                    AddCommandHistory($"Error message: `{e.Message}`");
                    s_currentSorterEnumm = null;
                }
            break;

            case StopScriptCommand:
                s_currentSorterEnumm = null;
            break;

            case CommandsCommand:
                AddCommandHistory("Commands:");
                foreach(var (name, aliases) in s_commands)
                {
                    AddCommandHistory($"\t{name}, {string.Join(',', aliases)}");
                }
            break;

            case ReloadScripsCommand:
                ReloadScripts();
            break;

            case OrderCommand:
                s_elementArray.Sort();
            break;

            case ChangeElemCountCommand c:
                ChangeElemCount(c);
            break;

            default:
                AddCommandHistory($"{s_currentCommand!.GetType().Name} WIP");
            break;
        }
        s_currentCommand = null;
    }

    static void CalculateCommandHistorySize()
    {
        var textSize = Helpers.GuiGetStyle(GuiControl.DEFAULT, GuiDefaultProperty.TEXT_SIZE);
        var linesSpacing = Helpers.GuiGetStyle(GuiControl.TEXTBOX, GuiTextBoxProperty.TEXT_LINES_SPACING);
        _commandHistorySize = (int)(_cmdHistBox.height / (textSize + linesSpacing));
    }

    static void AddCommandHistory(string cmd)
    {
        s_commandHistory.AddFirst(cmd);
        UpdateCommandHistory();
    }
    static void UpdateCommandHistory()
    {
        _inputHistory = "";

        int skip = 0;
        foreach (var cmd in s_commandHistory
                    .Skip(s_commandHistoryOffset)
                    .Take(_commandHistorySize)
                    .PadRightString(_commandHistorySize)
                    .Reverse())
        {

            if (skip-- > 0) continue;
            var lines = (cmd != "") ?
                (cmd.Length / s_maxTextLength) :
                0;
            skip += lines;
            int i = 0;
            for (; i < lines - 1; i++)
            {
                _inputHistory += cmd[(i * s_maxTextLength)..s_maxTextLength] + '\n';
            }
            _inputHistory += $"{cmd[i..]}\n";
        }
    }

    static void ReloadScripts()
    {
        AddCommandHistory("Reloading all scripts...");
        s_sorters.Clear();
        var def = ISorter.Default;
        s_sorters[def.Name] = def;
        LoadScripts();
    }

    static void LoadScripts()
    {
        foreach(var sourceFile in Directory.GetFiles(s_scriptsDirectoryLocation, "*.cs"))
        {
            try
            {
                var sorters = ScriptLoader.LoadFromFile(sourceFile);
                foreach (var sorter in sorters)
                {
                    if (s_sorters.ContainsKey(sorter.Name))
                    {
                        AddCommandHistory($"Sorter with name `{sorter.Name}` already loaded");
                    }
                    else
                    {
                        AddCommandHistory($"Loaded `{sorter.Name}` sorter from `{sourceFile}`");
                        s_sorters[sorter.Name] = sorter;
                    }
                }
            }
            catch(AggregateException ae)
            {
                AddCommandHistory($"`{sourceFile}` failed to compile.");
                foreach ( var ex in ae.InnerExceptions)
                {
                    AddCommandHistory(ex.Message);
                    Console.WriteLine(ae.Message);
                }
            }
            catch(LoadingException le)
            {
                AddCommandHistory(le.Message);
                Console.WriteLine(le.Message);
            }
            catch(Exception e)
            {
                AddCommandHistory(e.Message);
                Console.WriteLine($"ERROR:\t{e.ToString()}");
            }
        }
    }

    static void MeasureMaxTextLenght()
    {
        // A hack so I can estimate how many characters can fit in a single line
        var letterWidth = Raylib.MeasureTextEx(s_guiFont, "A", s_guiTextSize, s_guiTextSpacing).X;
        var maxText = (s_width / letterWidth);
        s_maxTextLength = (int)maxText;
    }

    static void ChangeElemCount(ChangeElemCountCommand cmd)
    {
        if(cmd.Amount <= 0)
        {
            AddCommandHistory("New element count must be greater than 0!");
            return;
        }
        s_currentSorterEnumm = null;
        s_elemCount = cmd.Amount;
        s_elementArray = new ElementArray(s_elemCount);
        s_sortingContext.TakeNewArray(s_elementArray);
        AddCommandHistory($"New element count is: {s_elemCount}");
    }
}
