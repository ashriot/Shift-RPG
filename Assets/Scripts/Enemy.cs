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
        "\t\tActions: " + string.Join(", ", actions.Select(a => a.name)) +
        "\nHP: " + hpCurrent + "/" + hpMax + "\t\tARM: " + (armorMax / 10) + "\t\tBRV: " + attack + "\t\tFTH: " + willpower +
        "\t\tSPD: " + speed + "\t\tCRT: " + crit + "%" + "\t\tDEF: " + (defense * 100) + "%\t\tRES: " + (resist * 100) + "%";
    }
  }
}
