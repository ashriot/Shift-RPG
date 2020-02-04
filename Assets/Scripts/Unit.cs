using UnityEngine;

public class Unit : ScriptableObject {
    
  public new string name;

  public int hpMax, armorMax, mp, mpRegen, attack, willpower, speed, crit, surge;
  public int hpCurrent, armorCurrent, mpCurrent;

  public float hpPercent { get { return (float)hpCurrent / hpMax; } }
  public float martialDefense, etherDefense, breakBonus;
    
  public bool isPlayer;
  public bool isDead { get { return hpCurrent <= 0; } }
}
