using UnityEngine;
using UnityEngine.UI;

public class PredictedPanelDisplay : MonoBehaviour {

  public Image icon;
  public Image ctbFill;
  public Image fillColor;
  public PredictedPanel predictedPanel;

  public void Click() {
    predictedPanel.panelRef.FlashImage();
  }
}
