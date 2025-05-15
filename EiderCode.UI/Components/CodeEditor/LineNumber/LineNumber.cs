using Godot;
using System;

namespace EiderCode.UI;

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

    public void SetLineNumber(int? value = null)
    {
      Number = value;
      if (_numberLabel == null) return;
      _numberLabel.Text = Number.HasValue ? Number.ToString() : "";
    }

    public void SetIsCursorOnLine(bool value)
    {
      Modulate = Modulate with { A = value ? 1f : 0.7f };
    }
}


public static class LineNumberBuilder
{
  public static MarginContainer Create(int? value, Theme theme, bool isCursorOnLine)
  {
    var container = new MarginContainer();
    container.AddThemeConstantOverride("margin_left", 10);
    container.AddThemeConstantOverride("margin_right", 10);
    container.CustomMinimumSize = new Vector2(50, 0);

    var label = new Label();
    label.Text = value.ToString();
    label.Theme = theme;
    label.HorizontalAlignment = HorizontalAlignment.Right;
    label.Modulate = label.Modulate with { A = isCursorOnLine ? 1f : 0.7f };
    container.AddChild(label);

    return container;
  }

  public static void UpdateNumber(MarginContainer container, int? value, bool isCursorOnLine)
  {
    var label = container.GetChild<Label>(0);
    label.Text = value.HasValue ? value.ToString() : "";
    label.Modulate = label.Modulate with { A = isCursorOnLine ? 1f : 0.7f };
  }
}