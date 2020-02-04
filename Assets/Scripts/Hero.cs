using UnityEngine;

[CreateAssetMenu(fileName = "Hero", menuName = "Hero")]
public class Hero : Unit {

  public Job[] jobs;
  public Job currentJob;

  public new float martialDefense { get { return currentJob.martialDefense; } }
  public new float etherDefense { get { return currentJob.etherDefense; } }
}
