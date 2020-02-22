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
  bool hovering = false;
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
    if (hovering) {
      if (Time.time - timePressStarted > holdTime) {
        ShowTooltip();
      }
    }
  }

  public override void OnPointerEnter(PointerEventData eventData) {
    if (showTooltip) {
      timePressStarted = Time.time;
      hovering = true;
    }
    base.OnPointerEnter(eventData);
  }

  public override void OnPointerExit(PointerEventData eventData) {
    hovering = false;
    OnRelease();
    base.OnPointerExit(eventData);
  }

  public override void OnPointerUp(PointerEventData eventData) {
    ShowTooltip();
    hovering = false;
    if (!interactable) return;
    OnRelease();
  }

  #endif


  #if !UNITY_STANDALONE

  void Update() {
    if ((isHeld) && !longPressTriggered) {
      if (Time.time - timePressStarted > holdTime) {
        longPressTriggered = true;
        OnLongPress();
      }
    }
  }

  public override void OnPointerDown(PointerEventData eventData) {
    longPressTriggered = false;
    if (showTooltip) {
      timePressStarted = Time.time;
      isHeld = true;
    }
  }

  public override void OnPointerUp(PointerEventData eventData) {
    if (longPressTriggered) {
      Tooltip.HideTooltip();
    }
    if (!interactable) return;
    if (!longPressTriggered) {
      ShowTooltip();
      onClick.Invoke();
    }
    OnRelease();
    longPressTriggered = false;
  }

  public override void OnPointerClick(PointerEventData eventData) {
    return;
  }

  #endif

  public void ShowTooltip() {
    if (!string.IsNullOrEmpty(_title))
      Tooltip.ShowTooltip(_title, _kind, _cost, _content, _job);
  }

  void OnLongPress(){
    isHeld = true;
    ShowTooltip();
  }

  void OnRelease() {
    isHeld = false;
    // Tooltip.HideTooltip();
  }
}