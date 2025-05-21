using EiderCode.Engine;

public enum Modifier {
  Around,
  Inner
}


public record ActionState
{
  public int Count = 1;
  public Action? CurrentAction = null;
  public Modifier? Modifier = null;
  public Motion? Motion = null;
}