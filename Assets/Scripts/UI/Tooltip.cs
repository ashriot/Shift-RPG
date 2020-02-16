using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour {

  public static Tooltip instance;

  public Camera cam;
  public Text title, kind, cost, content;

  public GameObject traitPanel, shiftPanel;
  public Image traitFill, traitIcon, shiftFill, shiftIcon;
  public Text traitText, shiftText;

  private Vector2 screenBounds;
  private float objectWidth;
  private float objectHeight;

  void Awake() {
    instance = this;
  }

  void Start() {
    screenBounds = Camera.main.ScreenToWorldPoint(
      new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z)
    );
  }

  private void LateUpdate() {
    objectWidth = instance.GetComponent<RectTransform>().sizeDelta.x;
    objectHeight = instance.GetComponent<RectTransform>().sizeDelta.y;
    var pt = Input.mousePosition - new Vector3(1900f/2f, 1000f/2f);
    pt.x = Mathf.Clamp(pt.x, -1920f, (Screen.width - (objectWidth * 2f + 10f)) / 2);
    pt.y = Mathf.Clamp(pt.y, -1080f,  Screen.height - (objectHeight * 1.5f + 10) / 2);
    transform.localPosition = pt;
  }

  public static void ShowTooltip(string title, string kind, string cost, string content, Job job = null) {
    instance.title.text = title;
    instance.kind.text = kind;
    if (cost != "NA") {
      instance.cost.text = (cost + " Mana").ToUpper();
    }
    else {
      instance.cost.text = string.Empty;
    }
    instance.content.text = content;

    if (job == null) {
      instance.traitPanel.SetActive(false);
      instance.shiftPanel.SetActive(false);
    } else {
      instance.traitPanel.SetActive(true);
      instance.shiftPanel.SetActive(true);

      instance.traitFill.color = job.jobColor;
      instance.traitIcon.sprite = job.trait.sprite;
      instance.traitText.text = job.trait.name;

      instance.shiftFill.color = job.jobColor;
      instance.shiftIcon.sprite = job.jobIcon;
      instance.shiftText.text = job.shiftAction.name;
    }

    instance.objectWidth = instance.GetComponent<RectTransform>().sizeDelta.x;
    instance.objectHeight = instance.GetComponent<RectTransform>().sizeDelta.y; 
    var pt = Input.mousePosition;
    pt.x = Mathf.Clamp(pt.x, 0f, Screen.width - (instance.objectWidth * 1.5f + 10f));
    pt.y = Mathf.Clamp(pt.y, 0f, Screen.height - (instance.objectHeight * 1.5f + 10));
    instance.transform.localPosition = pt;
    instance.gameObject.SetActive(true);
  }

  public static void HideTooltip() {
    instance.gameObject.SetActive(false);
  }

}
