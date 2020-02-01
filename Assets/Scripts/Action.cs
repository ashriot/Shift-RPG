using UnityEngine;

[CreateAssetMenu(fileName = "Action", menuName = "Action")]
public class Action : ScriptableObject {
    
  public new string name;
  public string description;
  public int cost;
  public int hits = 1;
  public int potency;
  public float delay = 1f;
  public Sprite sprite;
  public TargetTypes targetType;
  public DamageTypes damageType;

  public string sfxName;
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