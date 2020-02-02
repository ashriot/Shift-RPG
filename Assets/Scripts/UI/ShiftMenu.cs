using UnityEngine;
using UnityEngine.UI;

public class ShiftMenu : MonoBehaviour {

  public Button shiftL, shiftR;

  public Image colorL, colorR;
  public Text nameL, nameR;
  public Image jobIconL, jobIconR;

  public Vector2 initialSize;

  private void Start() {
    initialSize = GetComponent<RectTransform>().sizeDelta;
  }
}
