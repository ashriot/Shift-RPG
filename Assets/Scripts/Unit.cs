using UnityEngine;

public class Unit : ScriptableObject {
    
  public new string name;

  public int hp, armor, mp, strength, intellect, agility, crit, surge, armorClass;
  public int hpCurrent, armorCurrent, mpCurrent, ticks;

  public float hpPercent { get { return (float)hpCurrent / hp; } }
  public float mpRegen, martialDefense, etherDefense;
    
  public bool isPlayer;
  public bool isDead { get { return hpCurrent <= 0; } }
}
