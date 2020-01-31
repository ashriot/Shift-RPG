using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffect", menuName = "StatusEffect")]
public class StatusEffect : ScriptableObject {
  
  public new string name;
  public string description;
}

public enum StatusEffectTypes {
  Buff,
  Debuff
}
