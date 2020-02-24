using System.Collections.Generic;
using UnityEngine;

public class TurnOrder : MonoBehaviour {

  public Color black;
  public PredictedPanelDisplay[] panels;

  public void UpdateTurnOrder(List<PredictedPanel> _panels, decimal longestTick) {
    for (var i = 0; i < panels.Length; i++) {
      panels[i].predictedPanel = _panels[i];
      if (_panels[i].panelRef.GetType() == typeof(HeroPanel)) {
        var heroPanel = _panels[i].panelRef as HeroPanel;
        panels[i].icon.sprite = heroPanel.jobIcon.sprite;
        panels[i].icon.color = black;
        panels[i].fillColor.color = heroPanel.jobColor.color;
      } else {
        panels[i].icon.sprite = _panels[i].panelRef.image.sprite;
        panels[i].icon.color = Color.white;
        panels[i].fillColor.color = Color.gray;
      }
      panels[i].ctbFill.fillAmount = 1f - (float)(_panels[i].ticksUntilTurn / longestTick);
    }
  }
}
