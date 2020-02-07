using UnityEngine;

[CreateAssetMenu(fileName = "Trait", menuName = "Trait")]
public class Trait : ScriptableObject {
  public new string name;
  public string description;
  public Sprite sprite;
  public TraitTypes traitType;
}

public enum TraitTypes {
  Passive,
  Active
}
