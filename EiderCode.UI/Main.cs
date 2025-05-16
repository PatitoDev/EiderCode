using Godot;
using System;

namespace EiderCode.UI;

public partial class Main : Control
{
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
