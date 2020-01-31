using UnityEngine;

[CreateAssetMenu(fileName = "Hero", menuName = "Hero")]
public class Hero : Unit {

  public Job[] jobs;
  public Job currentJob;

  public bool isTaunting;
}
