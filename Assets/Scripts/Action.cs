using UnityEngine;

[CreateAssetMenu(fileName = "Action", menuName = "Action")]
public class Action : ScriptableObject {
    
  public new string name;
  public string description;
  public int cost;
  public int potency;
  public TargetTypes targetType;
  public DamageTypes damageType;
}

public enum TargetTypes {
  Self,
  OneAlly,
  SelfOrAlly,
  OtherAllies,
  EntireParty,
  OneEnemy,
  AllEnemies
}

public enum DamageTypes {
  Martial,
  Ether,
  Piercing
}