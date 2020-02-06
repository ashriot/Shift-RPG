using UnityEngine;

[CreateAssetMenu(fileName = "Hero", menuName = "Hero")]
public class Hero : Unit {

  public Job currentJob;
  public Job[] jobs;

  public override float Defense { get { return currentJob.defense; } }
  public override float Resist { get { return currentJob.resist; } }

  public bool shiftedThisTurn;
}
