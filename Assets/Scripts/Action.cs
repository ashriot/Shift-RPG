using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Action", menuName = "Action")]
public class Action : ScriptableObject {
    
  public new string name;
  public string description;
  public int mpCost;
  public float potency;
  public int hits = 1;
  public float delay = 1f;
  public bool splitDamage;
  public bool removesArmor = true;
  public Sprite sprite;
  public TargetTypes targetType;
  public DamageTypes damageType;
  public PowerTypes powerType;
  public List<Action> additionalDamage;
  public List<StatusEffect> buffs;
  public List<StatusEffect> debuffs;

  public string sfxName;

  public virtual bool Execute(Unit attacker, Unit defender) {
    var dealsDamage = true;

    return dealsDamage;
  }
}

public enum TargetTypes {
  Self,
  AlliesOnly,
  SelfOrAllies,
  WholeParty,
  RandomEnemies,
  OneEnemy,
  AllEnemies,
}

public enum DamageTypes {
  Martial,
  Ether,
  Piercing,
  ArmorGain,
  ManaGain,
  HealthGain
}

public enum PowerTypes {
  Attack,
  Willpower
}