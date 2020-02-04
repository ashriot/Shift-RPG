using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Enemy")]
public class Enemy : Unit {
  
  public Sprite sprite;
  public List<Action> actions;


  public string tooltipDescription {
    get {
      return description +
        "\nHP: " + hpCurrent + "/" + hpMax + "\t\t\t\tBase Armor: " + (armorMax / 10) +
        "\nAttack: " + attack + "\t\t\t\t\tPsyche: " + willpower +
        "\nSpeed: " + speed + "\t\t\t\t\tCrit: " + crit + "%" +
        "\nDefense: " + (martialDefense * 100) + "%\t\t\tResist: " + (etherDefense * 100) + "%" +
        "\nActions: " + string.Join(", ", actions.Select(a => a.name));
    }
  }
}
