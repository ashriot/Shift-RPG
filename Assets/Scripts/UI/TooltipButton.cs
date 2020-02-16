using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class TooltipButton : Button {

  public bool showTooltip = true;

  string _title, _kind, _cost, _content;
  Job _job = null;
  float holdTime = .5f;
  float timePressStarted;
  bool isHeld = false;
  bool isHover = false;
  bool longPressTriggered = false;

  public void SetupTooltip(string title, string kind, string cost, string content, Job job = null) {
    _title = title;
    _kind = kind;
    _cost = cost;
    _content = content;
    _job = job;
  }

#if UNITY_STANDALONE

  void Update() {
    if ((isHeld || isHover) && !longPressTriggered) {
      if (Time.time - timePressStarted > holdTime) {
        if (isHover) {
          Tooltip.ShowTooltip(_title, _kind, _cost, _content, _job);
        } else {
          longPressTriggered = true;
          OnLongPress();
        }
      }
    }
  }

  public override void OnPointerEnter(PointerEventData eventData) {
    if (showTooltip) {
      timePressStarted = Time.time;
      isHover = true;
      isHeld = false;
    }
    base.OnPointerEnter(eventData);
  }

  public override void OnPointerExit(PointerEventData eventData) {
    isHeld = false;
    isHover = false;
    if (longPressTriggered) {
      longPressTriggered = false;
    }
    OnRelease();
    base.OnPointerExit(eventData);
  }

  public override void OnPointerDown(PointerEventData eventData) {
    if (showTooltip) {
      timePressStarted = Time.time;
      if (!isHover) { isHeld = true; }
    }
  }

  public override void OnPointerUp(PointerEventData eventData) {
    if (!interactable) return;
    if (!longPressTriggered) {
      onClick.Invoke();
    }
      OnRelease();
    longPressTriggered = false;
  }

  public override void OnPointerClick(PointerEventData eventData) {
    return;
  }

  void OnLongPress(){
    isHeld = true;
    Tooltip.ShowTooltip(_title, _kind, _cost, _content, _job);
  }

  void OnRelease() {
    isHeld = false;
    Tooltip.HideTooltip();
  }

  #endif

  #if UNITY_IOS

  void Update() {

  }

  #endif
}