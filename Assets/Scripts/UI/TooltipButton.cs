using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class TooltipButton : Button {

  // public Color normalColor, highlightedColor, pressedColor, disabledColor;

  public string title, kind, cost, content;
  public Job job = null;

  private float holdTime = .5f;
  private float timePressStarted;
  private bool isHeld = false;
  private bool isHover = false;
  private bool longPressTriggered = false;

  private void Update() {
    if ((isHeld || isHover) && !longPressTriggered) {
      if (Time.time - timePressStarted > holdTime) {
        longPressTriggered = true;
        if (isHover) {
          Tooltip.ShowTooltip(title, kind, cost, content, job);
        } else {
        OnLongPress();
        }
      }
    }
  }

  public override void OnPointerEnter(PointerEventData eventData) {
    timePressStarted = Time.time;
    isHover = true;
    isHeld = false;
    base.OnPointerEnter(eventData);
  }

  public override void OnPointerExit(PointerEventData eventData) {
    isHeld = false;
    isHover = false;
    if (longPressTriggered) {
      OnRelease();
      longPressTriggered = false;
    }
    base.OnPointerExit(eventData);
  }

  public override void OnPointerDown(PointerEventData eventData) {
    timePressStarted = Time.time;
    if (!isHover) { isHeld = true; }
  }

  public override void OnPointerUp(PointerEventData eventData) {
    if (!longPressTriggered || !isHeld) {
      onClick.Invoke();
    }
      OnRelease();
    longPressTriggered = false;
  }

  public override void OnPointerClick(PointerEventData eventData) {
    return;
  }

  private void OnLongPress(){
    isHeld = true;
    Tooltip.ShowTooltip(title, kind, cost, content, job);
  }

  private void OnRelease() {
    isHeld = false;
    Tooltip.HideTooltip();
  }
}