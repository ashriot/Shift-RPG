using UnityEngine;
using UnityEngine.UI;

public class ActionMenu : MonoBehaviour {

  public ActionButton[] actionButtons;

  public Vector3 initialPos;

  public void DisplayActionMenu(Hero hero) {
    gameObject.SetActive(true);
    var mpModifier = (1 + hero.mpModifier);
    for (var i = 0; i < actionButtons.Length; i++) {
      if (i < hero.currentJob.actions.Length) {
        actionButtons[i].tooltipButton.SetupTooltip(hero.currentJob.actions[i].name, "Battle Action".ToUpper(), hero.currentJob.actions[i].mpCost.ToString(), hero.currentJob.actions[i].description);
        actionButtons[i].gameObject.SetActive(true);
        actionButtons[i].icon.GetComponent<Image>().sprite = hero.currentJob.actions[i].sprite;
        actionButtons[i].fillColor.color = hero.currentJob.jobColor;
        if (hero.currentJob.actions[i].mpCost * mpModifier <= hero.mpCurrent) {
          actionButtons[i].tooltipButton.interactable = true;
        } else {
          actionButtons[i].tooltipButton.interactable = false;
        }
        actionButtons[i].nameText.text = hero.currentJob.actions[i].name;
        actionButtons[i].mpCost = hero.currentJob.actions[i].mpCost;
        var crystals = Mathf.CeilToInt(actionButtons[i].mpCost * mpModifier / 10f);
        var leftover = (int)(actionButtons[i].mpCost * mpModifier) % 10;
        for (var m = 0; m < actionButtons[i].crystals.Length; m++) {
          if (m >= crystals) {
            actionButtons[i].crystals[m].gameObject.SetActive(false);
            continue;
          }
          if (m == (crystals - 1) && leftover > 0) {
            actionButtons[i].crystalFills[m].fillAmount = (float)(actionButtons[i].mpCost * mpModifier % 10) / 10f;
          } else {
            actionButtons[i].crystalFills[m].fillAmount = 1f;
          }
          actionButtons[i].crystals[m].gameObject.SetActive(true);
        }
      }
      else {
        actionButtons[i].gameObject.SetActive(false);
      }
    }
  }

}
