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
        "\nHP: " + hpCurrent + "/" + hpMax + "\t\t\t\tBase Armor: " + (armorMax / 10) +
        "\nAttack: " + attack + "\t\t\t\t\tPsyche: " + willpower +
        "\nSpeed: " + speed + "\t\t\t\t\tCrit: " + crit + "%" +
        "\nDefense: " + (defense * 100) + "%\t\t\tResist: " + (resist * 100) + "%" +
        "\nActions: " + string.Join(", ", actions.Select(a => a.name));
    }
  }
}
