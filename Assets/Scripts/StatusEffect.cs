using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffect", menuName = "StatusEffect")]
public class StatusEffect : ScriptableObject {
  
  public new string name;
  public string description;
  public int duration;
  public Sprite sprite;
  public StatusEffectTypes statusEffectType;
  public TargetTypes targetType;
  public Triggers trigger;
}

public enum StatusEffectTypes {
  Buff,
  Debuff
}

public enum Triggers
{
  StartOfTurn,
  EndOfTurn,
  BeingHit,
  BeingAttacked,
  PerHit,
  PerAttack
}
