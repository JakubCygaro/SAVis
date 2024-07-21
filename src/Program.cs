using SAVis.API;
using Raylib_CsLo;
using System.Reflection;
namespace SAVis;


internal class Program
{
    static SortingContext _sortingContext;
    static ElementArray _elementArray;

    const int _elemCount = 50;
    static int _width = 800;
    static int _height = 600;
    const int _fps = 60;
    const int _minWidth = 800;
    const int _minHeight = 600;
    const int _drawFrames = 1;
    const int _fontSize = 24;
    static int _commandHistorySize = 0;
    static int _commandHistoryPtr = -1;

    static int _commandHistoryOffset = 0;
    static Rectangle _cmdHistBox => new Rectangle()
    {
        width = _width, //(_width / 40) * 39,
        height = (_height / 4) - _fontSize + 2,
        x = 0,
        y = 0
    };

    static bool _drawOverlay = false;

    static Dictionary<string, ISorter> _sorters = new();

    static ISorter _currentSorter;
    static IEnumerator<bool>? _currentSorterEnumm;
    static Command? _currentCommand;

    static string _scriptsDirectoryLocation = "";
    static string _inputText = "";
    static string _inputHistory = "";
    static LinkedList<string> _commandHistory = new();
    static Program()
    {
        _elementArray = new(_elemCount);
        _sortingContext = new(_elementArray);
        _currentSorter = ISorter.Default;
        _sorters[_currentSorter.Name] = _currentSorter;
    }
    static void Main(string[] args)
    {
        SetupRaylib();
        SetupEnvironment();
        CalculateCommandHistorySize();

        while(!Raylib.WindowShouldClose())
        {
            Update();
            for(int i = 0; i < _drawFrames; i++)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Raylib.BLACK);
                Draw();
                if (_drawOverlay)
                    DrawOverlay();
                Raylib.EndDrawing();
            }
        }
    }
    static void SetupRaylib()
    {
        Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.InitWindow(_width, _height, "SAVis");
        Raylib.SetWindowMinSize(_minWidth, _minHeight);
        Raylib.SetTargetFPS(_fps);
        RayGui.GuiEnable();
        RayGui.GuiSetStyle((int)GuiControl.DEFAULT, (int)GuiControlProperty.TEXT_COLOR_NORMAL, 0xffffff);
        RayGui.GuiSetStyle((int)GuiControl.DEFAULT, (int)GuiControlProperty.TEXT_COLOR_FOCUSED, 0xffffff);
#if DEBUG
        Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_ALL);
#endif
        //RayGui.GuiLoadStyleDefault();
        //Raylib.InitAudioDevice();
    }
    static void SetupEnvironment()
    {
        var loc = Assembly.GetEntryAssembly()!.Location;
        loc = Directory.GetParent(loc)!.FullName;
        _scriptsDirectoryLocation = $"{loc}/scripts" ;
        _scriptsDirectoryLocation = Path.GetFullPath(_scriptsDirectoryLocation);
        Directory.CreateDirectory(_scriptsDirectoryLocation);
    }
    static void Update()
    {
        if (Raylib.IsWindowResized())
        {
            var handle = Raylib.GetCurrentMonitor();
            var (monW, monH) = (Raylib.GetMonitorWidth(handle), Raylib.GetMonitorHeight(handle));

            var (newW, newH) = (
                int.Clamp(Raylib.GetScreenWidth(), _minWidth, monW),
                int.Clamp(Raylib.GetScreenHeight(), _minHeight, monH)
                );

            _width = newW;
            _height = newH;
            CalculateCommandHistorySize();
            UpdateCommandHistory();
        }

        ProcessInput();
        if(_currentCommand is not null)
            ExcecuteCurrentCommand();
        _elementArray.WhiteAll();
        
        if(_currentSorterEnumm is not null)
        {
            if (_currentSorterEnumm.Current)
                _currentSorterEnumm = null;
            _currentSorterEnumm?.MoveNext();
        }

    }
    static void Draw()
    {
        var wRatio = _width / (float)_elemCount;
        var hRatio = _height / (float)_elemCount;
        int index = 0;
        foreach( (uint, ElementColor) elem in _elementArray )
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
                    y = _height - hRatio * elem.Item1,
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
            width = _width,
            height = _height / 4,
        },
        new Color(0, 0, 0, 200));

        RayGui.GuiTextBox(
            new Rectangle()
            {
                width = _width,
                height = _fontSize + 2,
                x = 0,
                y = (_height / 4) - _fontSize + 2,
            }, _inputText, _fontSize, true);

        RayGui.GuiTextBoxMulti(_cmdHistBox, _inputHistory, _fontSize, false);

        //(_width / 40)

        //if((_v = RayGui.Scr(
        //    new Rectangle()
        //    {
        //        x = (_width / 40) * 39,
        //        y = 0,
        //        width = (_width / 40),
        //        height = _cmdHistBox.height
        //    }, "", "", _v, 0, 100)) != 0)
        //{
        //    Console.WriteLine(_v);
        //}
    }

    static void ProcessInput()
    {
        if(Raylib.IsKeyReleased(KeyboardKey.KEY_GRAVE))
        {
            _drawOverlay = !_drawOverlay;
        }
        if (_drawOverlay)
        {
            ReadInputText();
            var mouseWheel = Raylib.GetMouseWheelMove();
            _commandHistoryOffset = int.Clamp((int)(_commandHistoryOffset + mouseWheel), 0, _commandHistory.Count);
            UpdateCommandHistory();
        }
    }

    static void ReadInputText()
    {
        int key;
        while((key = Raylib.GetCharPressed()) > 0)
        {
            _inputText += (char)key;
        }
        if(Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE))
        {
            if(_inputText.Length > 0)
                _inputText = _inputText[..^1];
            _commandHistoryPtr = -1;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
        {
            try
            {
                _currentCommand = Command.ParseFromString(_inputText);
                AddCommandHistory(_inputText);
            }
            catch(CommandParsingException cpm)
            {
                AddCommandHistory(cpm.Message);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                _inputText = "";
                _commandHistoryPtr = -1;
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
        {
            _commandHistoryPtr++;
            var elem = _commandHistory.ElementAtOrDefault(_commandHistoryPtr);
            if (elem is not null)
            {
                _inputText = elem;
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
        {
            _commandHistoryPtr--;
            _commandHistoryPtr = int.Clamp(_commandHistoryPtr, -1, _commandHistory.Count);
            var elem = _commandHistory.ElementAtOrDefault(_commandHistoryPtr);
            if (elem is not null)
            {
                _inputText = elem;
            }
        }

    }

    static void ExcecuteCurrentCommand()
    {
        switch(_currentCommand)
        {
            case ShuffleCommand:
                _elementArray.Shuffle();
            break;

            case LoadScriptsCommand:
                LoadScripts();
            break;

            case ListScriptsCommand:
                AddCommandHistory("Currently loaded sorter scripts:");
                int i = 1;
                foreach(var (name, sorter) in _sorters)
                {
                    AddCommandHistory($"\t {i++}. {name} by {sorter.Author}");
                }
            break;

            case SelectScriptCommand select:
                if (_sorters.GetValueOrDefault(select.Script) is ISorter selection)
                {
                    _currentSorter = selection;
                    AddCommandHistory($"Selected `{selection.Name}`");
                }
                else
                {
                    AddCommandHistory($"No such script `{select.Script}`");
                }
            break;

            case PlayScriptCommand:
                _currentSorterEnumm = _currentSorter.Update(_sortingContext);
                AddCommandHistory($"Now playing `{_currentSorter.Name}`");
            break;

            case StopScriptCommand:
                _currentSorterEnumm = null;
            break;

            default:
                AddCommandHistory($"{_currentCommand!.GetType().Name} WIP");
            break;
        }
        _currentCommand = null;
    }

    static void CalculateCommandHistorySize()
    {
        var textSize = Helpers.GuiGetStyle(GuiControl.DEFAULT, GuiDefaultProperty.TEXT_SIZE);
        var linesSpacing = Helpers.GuiGetStyle(GuiControl.TEXTBOX, GuiTextBoxProperty.TEXT_LINES_SPACING);
        _commandHistorySize = (int)(_cmdHistBox.height / (textSize + linesSpacing));
    }

    static void AddCommandHistory(string cmd)
    {
        _commandHistory.AddFirst(cmd);
        UpdateCommandHistory();
    }
    static void UpdateCommandHistory()
    {
        _inputHistory = "";
        foreach (var cmd in _commandHistory
                    .Skip(_commandHistoryOffset)
                    .Take(_commandHistorySize)
                    .PadRightString(_commandHistorySize)
                    .Reverse())
        {
            _inputHistory += $"{cmd}\n";
        }
    }

    static void LoadScripts()
    {
        foreach(var sourceFile in Directory.GetFiles(_scriptsDirectoryLocation, "*.cs"))
        {
            try
            {
                var sorters = ScriptLoader.LoadFromFile(sourceFile);
                foreach (var sorter in sorters)
                {
                    AddCommandHistory($"Loaded `{sorter.Name}` sorter from `{sourceFile}`");
                    if (_sorters.ContainsKey(sorter.Name))
                    {
                        AddCommandHistory($"Sorter with name `{sorter.Name}` already loaded");
                    }
                    else
                    {
                        _sorters[sorter.Name] = sorter;
                    }
                }
            }
            catch(LoadingException le)
            {
                AddCommandHistory(le.Message);
            }
            catch(Exception e)
            {
                AddCommandHistory(e.Message);
                Raylib.TraceLog(TraceLogLevel.LOG_DEBUG, e.ToString());
            }
        }
    }
}
