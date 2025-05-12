using Godot;
using System;

public partial class LineNumber : MarginContainer
{
    public int? Number { get; private set; }
    private Label? _numberLabel;

    public override void _Ready()
    {
      base._Ready();
      _numberLabel = GetNode<Label>("%NumberLabel");
      _numberLabel.Text = Number.HasValue ? Number.ToString() : "";
      Modulate = Modulate with { A = 0.7f };
    }

    public void SetLineNumber(int? value = null){
      Number = value;
      if (_numberLabel == null) return;
      _numberLabel.Text = Number.HasValue ? Number.ToString() : "";
    }
}
