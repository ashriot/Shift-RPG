using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Enemy")]
public class Enemy : Unit {
  
  public Sprite sprite;
  public List<Action> actions;

  public override float Defense { get { return defense; } }
  public override float Resist { get { return resist; } }

  public string tooltipDescription {
    get {
      return description +
        "\nHP: " + hpCurrent + "/" + hpMax + "\t\tARM: " + (armorMax / 10) + "\t\tATK: " + attack + "\t\tPSY: " + willpower +
        "\nSPD: " + speed + "\t\tCRT: " + crit + "%" + "\t\tDEF: " + (defense * 100) + "%\t\tRES: " + (resist * 100) + "%" +
        "\nActions: " + string.Join(", ", actions.Select(a => a.name));
    }
  }
}
