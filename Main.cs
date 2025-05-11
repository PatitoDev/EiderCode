using Godot;
using System;

public partial class Main : Control
{
    private FileExplorer explorer;

    public override void _Ready()
    {
        base._Ready();
        var codeEditor = GetNode<CodeEditor>("%CodeEditor");

        explorer = GetNode<FileExplorer>("%FileExplorer");
        explorer.OnFileOpen += (filePath) =>
        {
            codeEditor.OpenFile(filePath);
        };

    }
}
