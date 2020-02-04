using UnityEngine;
using UnityEngine.UI;

public class ShiftMenu : MonoBehaviour {

  public Button shiftL, shiftR;

  public Image colorL, colorR, traitColor, shiftColor;
  public Text nameL, nameR, traitName, shiftName;
  public Image jobIconL, jobIconR, traitIcon, shiftIcon;
  public int jobIdL, jobIdR;
  public TooltipButton traitTooltipButton, shiftActionTooltipButton, shiftLTooltipButton, shiftRTooltipButton;

  public Vector2 initialSize;

  private void Start() {
    initialSize = GetComponent<RectTransform>().sizeDelta;
  }

  public void Click(int buttonId) {
    var jobId = 0;
    if (buttonId == 0) {
      jobId = jobIdL;
    } else {
      jobId = jobIdR;
    }
    BattleManager.instance.ClickShiftButton(jobId);
  }
}
