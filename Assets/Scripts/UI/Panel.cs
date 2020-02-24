using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Panel : MonoBehaviour {

  public Unit unit;
  public Text currentHp;
  public ShakeObject shaker;
  public TooltipButton targetButton;
  public TooltipButton infoTooltip;
  public Image targetCursor;
  public bool panelMoved;

  public Image hpFillImage, image;
  public Image[] crystals, crystalFills;
  public ShieldIcon[] shields;
  public StaggerIcon staggerIcon;
  public PulsingText exposedText;

  public GameObject hud;
  public StatusEffectPanel buffs, debuffs;

  public List<StatusEffectDisplay> buffsOnOthers = new List<StatusEffectDisplay>();
  public List<StatusEffectDisplay> debuffsOnOthers = new List<StatusEffectDisplay>();

  public float moveLifetime, fadeLifetime, flashLifetime;
  public float initialSpeed;
  public Text notificationText;

  public new string name { get { return unit.name; } }
  public bool updateHpBar, isStunned, taunting;
  public bool exposed { get { return remainingStaggeredTurns > 0; } }
  public int remainingStaggeredTurns;
  public int staggerDelayAmount { get { return unit.staggerDelayAmount; } }
  public bool isDead { get { return unit.isDead; } }
  public decimal ticks;

  public float damageDealtPercentMod, damageTakenPercentMod, speedMod;
  public int damageDealtFlatMod;

  public Color red, gray, gold;

  private Vector3 movePosition;
  private Vector3 initialPos;

  private bool isMoving, isFading;

  private float intensity, speed, timer, alpha, exposedTimer;
  private Color fade;

  void Update() {
    if (isMoving) {
      if (timer >= moveLifetime) {
        isMoving = false;
      }
      transform.localPosition = Vector3.MoveTowards(transform.localPosition, movePosition, speed * Time.deltaTime);
      timer += Time.deltaTime;
    } else if (isFading) {
      image.color = new Color(image.color.r, image.color.g, image.color.b,
          Mathf.MoveTowards(image.color.a, 0f, ( 1 / fadeLifetime) * Time.deltaTime));
          
      if (image.color.a == 0f) {
          isFading = false;
          image.gameObject.SetActive(false);
          hud.SetActive(false);
          buffs.gameObject.SetActive(false);
          debuffs.gameObject.SetActive(false);
      }
    }
  }

  public virtual void Setup() {
    unit.hpCurrent = unit.hpMax;
    unit.armorCurrent = unit.armorMax;
    unit.mpCurrent = unit.mp;
    hpFillImage.fillAmount = unit.hpPercent;
    UpdateCrystalsAndShields();
  }

  public void UpdateCrystalsAndShields() {
    var mpCrystals = Mathf.CeilToInt(unit.mpCurrent / 10f);
    var leftover = (int)unit.mpCurrent % 10;
    for (var m = 0; m < crystals.Length; m++) {
      if (m >= mpCrystals) {
        crystals[m].gameObject.SetActive(false);
        continue;
      }
      if (m == (mpCrystals - 1) && leftover > 0) {
        crystalFills[m].fillAmount = (float)leftover / 10f;
      } else {
        crystalFills[m].fillAmount = 1f;
      }
      crystals[m].gameObject.SetActive(true);
    }
    var shieldColor = gray;
    if (exposed) {
      shieldColor = red;
      exposedText.gameObject.SetActive(true);
      exposedText.Pulse(BattleManager.instance.battleSpeed);
      unit.armorCurrent = unit.armorMax;
    } else {
      exposedText.gameObject.SetActive(false);
    }
    var armorShields = unit.armorCurrent / 10;
    var secondShields = 0;
    var thirdShields = 0;
    if (armorShields > 10) {
      secondShields = armorShields - 10;
      armorShields = 10;
      if (secondShields > 10) {
        thirdShields = secondShields - 10;
        secondShields = 10;
      }
    }
    for (var m = 0; m < shields.Length; m++) {
      if (m >= armorShields) {
        shields[m].gameObject.SetActive(false);
        continue;
      }
      if (!exposed && m < secondShields) {
        shieldColor = gold;
      } else if (!exposed) {
        shieldColor = gray;
      }
      shields[m].gameObject.SetActive(true);
      shields[m].shieldIcon.color = shieldColor;
    }
  }

  public void Move(float distance, float duration) {
    timer = 0f;
    // Debug.Log("distance: " + distance + " " + transform.localPosition + " -> " + movePosition);
    movePosition = transform.localPosition + new Vector3(0f, distance, 0f);
    // Debug.Log("distance: " + distance + " " + transform.localPosition + " -> " + movePosition);
    speed = initialSpeed * (1 / duration);
    moveLifetime = duration;
    isMoving = true;
  }

  public void Click() {
    BattleManager.instance.ClickTarget(this);
  }

  public void FadeOut(float duration) {
    timer = 0f;
    fadeLifetime = duration;
    isFading = true;
  }
}

public class PredictedPanel {
  
  public decimal ticksUntilTurn;
  public Panel panelRef;

  public PredictedPanel(decimal ticks, ref Panel panel) {
    ticksUntilTurn = ticks;
    panelRef = panel;
  }
}
