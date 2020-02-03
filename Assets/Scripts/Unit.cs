using System.Collections.Generic;
using UnityEngine;

public class Unit : ScriptableObject {
    
  public new string name;

  public int hp, armor, mp, mpRegen, attack, willpower, speed, crit, surge, armorClass;
  public int hpCurrent, armorCurrent, mpCurrent, ticks;

  public float hpPercent { get { return (float)hpCurrent / hp; } }
  public float martialDefense, etherDefense, breakBonus;

  public List<StatusEffect> buffs;
  public List<StatusEffect> debuffs;
    
  public bool isPlayer, isArmorBroke;
  public bool isDead { get { return hpCurrent <= 0; } }
}
