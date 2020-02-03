using UnityEngine;
using UnityEngine.UI;

public class ShiftMenu : MonoBehaviour {

  public Button shiftL, shiftR;

  public Image colorL, colorR, traitColor, shiftColor;
  public Text nameL, nameR, traitName, shiftName;
  public Image jobIconL, jobIconR, traitIcon, shiftIcon;

  public Vector2 initialSize;

  private void Start() {
    initialSize = GetComponent<RectTransform>().sizeDelta;
  }
}
