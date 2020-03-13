using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "Skill")]
public class Skill : ScriptableObject {
  public new string name;
  public string description;
  public SkillNodeTypes skillNodeType;
  public int amount; 

}

public enum SkillNodeTypes {
  hp,
  mp,
  str,
  mag,
  ard,
  spd,
  crit,
  aim,
  def,
  res,
  action,
  shift,
  talent,
  perk,
  rank,
  acc,
  arm,
  unique
}
