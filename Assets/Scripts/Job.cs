using UnityEngine;

[CreateAssetMenu(fileName = "Job", menuName = "Job")]
public class Job : ScriptableObject {

  public new string name;
  public string description;
  public Sprite jobIcon;
  public Color jobColor;
  public float martialDefense, etherDefense;

  public Action[] actions;
}
