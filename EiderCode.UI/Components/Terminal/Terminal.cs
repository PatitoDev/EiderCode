using Godot;
using System;

namespace EiderCode.UI;

public partial class Terminal : Control
{
    private string testDirPath = "D:/DevStream/ElPatoDraw_/Front UI";

    private Godot.Collections.Dictionary pdict;

    private int processId;
    private FileAccess stdio;
    private FileAccess stderr;

    private RichTextLabel textLabel;

    public override void _Ready()
    {
        var pshell = OS.ExecuteWithPipe("Powershell.exe", [
          "-noexit",
      "Set-Location",
      testDirPath
        ], false);

        pdict = pshell;
        stdio = (FileAccess)pshell["stdio"];
        stderr = (FileAccess)pshell["stderr"];
        processId = (int)pshell["pid"];
        var output = stdio.GetAsText();
        textLabel.AddText(output);

        GD.Print("ready");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!stdio.IsOpen())
        {
            GD.Print("end of file");
            return;
        }

        //stdio.StoreLine("Write-Host 'Hello, World!' -ForegroundColor Cyan");
        var output = stdio.GetAsText();
        if (!String.IsNullOrEmpty(output))
        {
            textLabel.AddText(output);
        }
    }
}
