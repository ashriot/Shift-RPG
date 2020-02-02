using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Enemy")]
public class Enemy : Unit {
  
  public Sprite sprite;
  public List<Action> actions;

}
