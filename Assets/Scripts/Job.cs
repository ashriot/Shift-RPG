using UnityEngine;

[CreateAssetMenu(fileName = "Job", menuName = "Job")]
public class Job : ScriptableObject {

  public new string name;
  public string description;
  public Sprite jobIcon;
  public Color jobColor;
  public float defense, resist;

  public Action[] actions;
  public StatusEffect trait;
  public Action shiftAction;

  public Skill[] skills;
}
