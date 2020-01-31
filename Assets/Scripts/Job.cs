using UnityEngine;

[CreateAssetMenu(fileName = "Job", menuName = "Job")]
public class Job : ScriptableObject {

  public new string name;
  public float martialDefense, etherDefense;

  public Action[] actions;
}
