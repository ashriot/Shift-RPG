using UnityEngine;

[CreateAssetMenu(fileName = "Hero", menuName = "Hero")]
public class Hero : Unit {

  public Job[] jobs;
  public Job currentJob;

  public override float Defense { get { return currentJob.defense; } }
  public override float Resist { get { return currentJob.resist; } }

  public bool shiftedThisTurn;
}
