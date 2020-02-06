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
        "\nHP: " + hpCurrent + "/" + hpMax + "\t\tBase Armor: " + (armorMax / 10) +
        "\t\tAttack: " + attack + "\t\tPsyche: " + willpower +
        "\t\tSpeed: " + speed + "\t\tCrit: " + crit + "%" +
        "\t\tDefense: " + (defense * 100) + "%\t\tResist: " + (resist * 100) + "%";
    }
  }
}
