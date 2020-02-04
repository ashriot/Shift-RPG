using UnityEngine;

public abstract class Unit : ScriptableObject {
    
  public new string name;
  public string description;

  public int hpMax, armorMax, mp, mpRegen, attack, willpower, speed, crit, surge;
  public int hpCurrent, armorCurrent, mpCurrent;

  public float hpPercent { get { return (float)hpCurrent / hpMax; } }
  public float defense, resist, breakBonus;
    
  public bool isPlayer;
  public bool isDead { get { return hpCurrent <= 0; } }

  public abstract float Defense { get; }
  public abstract float Resist { get; }
}
