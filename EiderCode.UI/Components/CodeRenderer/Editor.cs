using EiderCode.Engine;
using Godot;
using System.Threading;
using System.Threading.Tasks;

public partial class Editor : MarginContainer
{
    private CodeEngine _codeEngine = new();
    private TextServer _textServer;

    private int _fontSize;
    private Font _font;
    private Vector2 _charSize;

    private CodeRenderer? _codeRenderer;
    private GutterRenderer? _gutterRenderer;

    public Editor()
    {
        _fontSize = Theme.DefaultFontSize;
        _font = Theme.DefaultFont;
        _textServer = TextServerManager.GetPrimaryInterface();

        // assume font is monospace
        _charSize = _font.GetStringSize(
          "A",
          HorizontalAlignment.Left,
          -1,
          Theme.DefaultFontSize
        );
    }

    private CancellationTokenSource? OpenFileCancellationTokenSource;

    private CancellationTokenSource? ContentUpdateCancellationTokenSource;

    public override void _Ready()
    {
        //GetNode<ColorRect>("%BGRect").Color = _codeEngine.GetGuiColor(GuiThemeKeys.EditorBg);
        _codeRenderer = GetNode<CodeRenderer>("%CodeRenderer");
        _gutterRenderer = GetNode<GutterRenderer>("%GutterRenderer");

        _codeRenderer._fontSize = _fontSize;
        _codeRenderer._font = _font;
        _codeRenderer._charSize = _charSize;
        _codeRenderer._codeEngine = _codeEngine;
        _codeRenderer._textServer = _textServer;

        _gutterRenderer._fontSize = _fontSize;
        _gutterRenderer._font = _font;
        _gutterRenderer._charSize = _charSize;
        _gutterRenderer._codeEngine = _codeEngine;
        _gutterRenderer._textServer = _textServer;

        _gutterRenderer.SetupListeners();
        _codeRenderer.SetupListeners();
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (
            @event is InputEventMouse &&
            DisplayServer.MouseGetMode() != DisplayServer.MouseMode.Visible
        )
        {
            DisplayServer.MouseSetMode(DisplayServer.MouseMode.Visible);
        }

        if (
            @event is InputEventKey &&
            ((InputEventKey)@event).IsPressed()
        )
        {
            DisplayServer.MouseSetMode(DisplayServer.MouseMode.Hidden);
            var inputEventKey = ((InputEventKey)@event);

            _codeEngine?.HandleKeyPress(new(){
                IsShiftPressed = inputEventKey.ShiftPressed,
                IsControlPressed = inputEventKey.CtrlPressed,
                KeyCode = inputEventKey.PhysicalKeycode,
                Unicode = inputEventKey.Unicode == 0 ? null : inputEventKey.Unicode
            });
        }
    }

    public void OpenFile(string filePath)
    {
        if (OpenFileCancellationTokenSource != null)
        {
            OpenFileCancellationTokenSource.Cancel();
        }
        OpenFileCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = OpenFileCancellationTokenSource.Token;

        GrabFocus();
        _codeRenderer!.UpdateCursorPosition();

        //_codeEngine.ClearOnLineParsedEvent();

        Task.Run(async () =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            _codeRenderer?.OnFileOpen();
            _gutterRenderer?.OnFileOpen();
            if (cancellationToken.IsCancellationRequested) return;
            await _codeEngine.OpenFileAsync(filePath, cancellationToken);
        });
    }
}
