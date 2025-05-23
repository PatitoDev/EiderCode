using System.Runtime.InteropServices;
using Godot;

namespace EiderCode.UI;

public partial class Main : PanelContainer
{
    #if GODOT_WINDOWS

    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();
    #endif

    public Main(): base()
    {
        #if GODOT_WINDOWS
        AllocConsole();
        #endif
    }

    private FileExplorer? explorer;


    public override void _Ready()
    {
        base._Ready();

        // handle combination keys like accents
        GetWindow().SetImeActive(true);

        //var codeEditor = GetNode<CodeEditor>("%CodeEditor");
        var editor = GetNode<Editor>("%Editor");

        explorer = GetNode<FileExplorer>("%FileExplorer");
        explorer.OnFileOpen += (filePath) =>
        {
            //codeEditor.OpenFile(filePath);
            editor.OpenFile(filePath);
        };

    }
}
