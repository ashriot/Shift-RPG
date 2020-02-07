using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour {

  public static BattleManager instance;

  [Header("Object Refs")]
  public Canvas canvas;
  public ActionMenu actionMenu;
  public ShiftMenu shiftMenu;
  public Tooltip tooltip;

  [Header("Prefabs")]
  public PopupText popupPrefab;
  public PopupText damagePrefab;
  public StatusEffectDisplay buffPrefab;
  public StatusEffectDisplay debuffPrefab;

  [Header("Hero/Enemy Refs")]
  public HeroPanel[] heroPanels;
  public EnemyPanel[] enemyPanels;
  public List<HeroPanel> heroes;
  public List<EnemyPanel> enemies;
  public List<Enemy> enemyLoadList;

  [Header("Control Vars")]
  public float battleSpeed;
  public bool battleActive;
  public bool battleWaiting;
  public bool choosingTarget;
  public bool targetingShiftAction;
  public bool delaying;
  public bool pausedForStagger;
  public bool pausedForTriggers;
  public List<Panel> combatants = new List<Panel>();
  public Panel currentPanel;
  public Panel playerTarget;
  public HeroPanel enemyTarget;
  public Action currentAction;

  [Header("Colors & Icons")]
  public Sprite heart;
  public Sprite crystal, shieldSprite;
  public Color orange, purple, yellow, gray, black, blue, green;

  private const float INITIATIVE_GROWTH = .05f;
  private const float SHAKE_INTENSITY = 20f;

  private void Awake() {
    instance = this;
    DontDestroyOnLoad(gameObject);
  }
  private void Start() {
    actionMenu.gameObject.SetActive(false);
    foreach(var panel in enemyPanels) {
      panel.gameObject.SetActive(false);
    }

    shiftMenu.selectTarget.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    Tooltip.HideTooltip();
    InitializeBattle();
  }

  public void InitializeBattle() {
    if (battleActive) return;

    AudioManager.instance.PlayBgm("battle-conflict");
    battleActive = true;

    for (var i = 0; i < heroPanels.Length; i++) {
      if (i >= GameManager.instance.heroes.Count) { 
        heroPanels[i].gameObject.SetActive(false);
        heroPanels[i].GetComponent<RectTransform>().gameObject.SetActive(false);
        continue;
      }

      var panel = heroPanels[i];

      panel.gameObject.SetActive(true);
      panel.GetComponent<RectTransform>().gameObject.SetActive(true);
      panel.unit = GameManager.instance.heroes[i];
      var hero = panel.unit as Hero;
      panel.jobColor.color = hero.currentJob.jobColor;
      panel.jobIcon.sprite = hero.currentJob.jobIcon;
      panel.unit.hpCurrent = panel.unit.hpMax;
      panel.unit.armorCurrent = panel.unit.armorMax;
      panel.unit.mpCurrent = panel.unit.mp;
      panel.unitNameText.text = panel.unit.name;
      panel.currentJobName.text = hero.currentJob.name.ToUpper();
      panel.hpFillImage.fillAmount = panel.unit.hpPercent;
      panel.currentHp.text = panel.unit.hpMax.ToString();
      UpdateCrystalsAndShields(panel);
    }

    for(var i = 0; i < enemyPanels.Length; i++) {
      if (i >= enemyLoadList.Count) { 
        enemyPanels[i].gameObject.SetActive(false);
        continue;
      } else {
        var panel = enemyPanels[i] as EnemyPanel;
        panel.gameObject.SetActive(true);
        panel.unit = Instantiate(enemyLoadList[i]);
        var enemy = panel.unit as Enemy;
        panel.image.sprite = enemy.sprite;
        panel.unit.hpCurrent = panel.unit.hpMax;
        panel.unit.armorCurrent = panel.unit.armorMax;
        panel.unit.mpCurrent = panel.unit.mp;
        panel.hpFillImage.fillAmount = panel.unit.hpPercent;
        UpdateCrystalsAndShields(panel);
        SetupTooltip(panel.infoTooltip, enemy.name, "Enemy".ToUpper(), "NA", enemy.tooltipDescription);
      }
    }
    heroes = heroPanels.Where(p => p.gameObject.activeInHierarchy).ToList();
    enemies = enemyPanels.Where(p => p.gameObject.activeInHierarchy).ToList();

    combatants.AddRange(heroes);
    combatants.AddRange(enemies);

    ClearAllTargetting();
    StartCoroutine(DoDelay(0.5f));
    SetInitialTicks();
    foreach (var heroPanel in heroes) {
      StartCoroutine(DoHandleTraits(heroPanel));
    }
    NextTurn();
  }

  private void UpdateCrystalsAndShields(Panel panel) {
    var crystals = panel.unit.mpCurrent / 10;
    for (var m = 0; m < panel.crystals.Length; m++) {
      if (m >= crystals) {
        panel.crystals[m].gameObject.SetActive(false);
        continue;
      }
      panel.crystals[m].gameObject.SetActive(true);
    }
    var shieldColor = gray;
    if (panel.isStaggered) {
      shieldColor = Color.red;
      panel.unit.armorCurrent = panel.unit.armorMax;
    }
    var shields = panel.unit.armorCurrent / 10;
    for (var m = 0; m < panel.shields.Length; m++) {
      if (m >= shields) {
        panel.shields[m].gameObject.SetActive(false);
        continue;
      }
      panel.shields[m].gameObject.SetActive(true);
      panel.shields[m].shieldIcon.color = shieldColor;
    }
  }

  private void Update() {
    foreach(var panel in heroes) {
      if (panel.updateHpBar) {
        SmoothHpBar(panel, panel.unit.hpPercent);
      }
    }
    foreach (var panel in enemies) {
      if (panel.updateHpBar) {
        SmoothHpBar(panel, panel.unit.hpPercent);
      }
    }
  }

  void FixedUpdate() {
    if (!battleActive || !battleWaiting || delaying || pausedForStagger || pausedForTriggers || choosingTarget) { return; }
    battleWaiting = false;

    // check status effects

    if (combatants.All(x => x.unit.isDead && !x.unit.isPlayer)) return;

    currentPanel.unit.mpCurrent += currentPanel.unit.mpRegen;
    UpdateUi();
    if (currentPanel.isStaggered) {
        currentPanel.isStaggered = false;
        var position = currentPanel.gameObject.transform.position;
        position.y += currentPanel.GetComponent<RectTransform>().rect.height;
        Instantiate(popupPrefab, position, currentPanel.gameObject.transform.rotation, canvas.transform).DisplayMessage("Recovered", shieldSprite, battleSpeed * 0.8f, Color.white, false);
        UpdateUi();
      }
    if (currentPanel.isStunned) {
      CalculateSpeedTicks(currentPanel, 1f);
      currentPanel.isStunned = false;
      NextTurn();
      return;
    }

    currentPanel.dmgIncreasePercentMod = 0f;
    currentPanel.dmgReductionPercentMod = 0f;
    currentPanel.dmgIncreaseFlatMod = 0;

    StartCoroutine(DoHandleStatusEffects(currentPanel, TriggerTypes.StartOfTurn));
    if (currentPanel.unit.isPlayer) {
      // Debug.Log("Player turn");
      // HERO TURN
      var hero = currentPanel.unit as Hero;
      hero.shiftedThisTurn = false;
      shiftMenu.shiftLTooltipButton.interactable = true;
      shiftMenu.shiftRTooltipButton.interactable = true;
      MovePanel(currentPanel, true);
      SlideHeroMenus(hero, true);

    } else {
      // Debug.Log("Enemy turn");
      // ENEMY TURN

      var taunters = heroes.Where(h => h.isTaunting).ToList();
      if (taunters.Count > 0) {
        enemyTarget = taunters.First();
      } else {
        // random target
        // var random = Random.Range(0, heroes.Count);
        var random = 0;
        enemyTarget = heroes[random];
      }
      EnemyAction();
    }
  }

  private void SetInitialTicks() {
      foreach (var c in combatants) {
        var rand = Random.Range(0.5f, 1.5f);
        CalculateSpeedTicks(c, rand);
      }
  }

  private void NextTurn() {
    ResetActions();
    playerTarget = null;
    actionMenu.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    // check for battle end

    // UpdatePlayerTarget();

    CountdownTicks();
  }

  private void UpdateUi() {
    for (var i = 0; i < heroes.Count; i++) {
      var panel = heroPanels[i];
      if (panel.unit.armorCurrent < 0 && !panel.isStaggered && !panel.unit.isDead) {
        panel.isStaggered = true;
        ShakePanel(heroes[i], SHAKE_INTENSITY, 1.5f);
        AudioManager.instance.PlaySfx("damage02");
        Debug.Log("Stagger!");
        var position = panel.gameObject.transform.position;
        position.y += panel.GetComponent<RectTransform>().rect.height;
        Instantiate(popupPrefab, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage("Staggered!", shieldSprite, battleSpeed * 0.8f, Color.white, false);
        panel.isStunned = true;
        CalculateSpeedTicks(panel, 1f);
        StartCoroutine(DoFlashImage(panel.image, Color.red));
        StartCoroutine(DoPauseForStagger());
      }
      var hero = panel.unit as Hero;
      panel.currentJobName.text = hero.currentJob.name.ToUpper();
      panel.jobColor.color = hero.currentJob.jobColor;
      panel.jobIcon.sprite = hero.currentJob.jobIcon;
      panel.updateHpBar = true;
      panel.currentHp.text = panel.unit.hpCurrent.ToString();
      UpdateCrystalsAndShields(panel);
    }

    for(var i = 0; i < enemies.Count; i++) {
      var panel = enemies[i];
      if (panel.unit.armorCurrent <= -10 && !panel.isStaggered) {
        panel.isStaggered = true;
        ShakePanel(panel, SHAKE_INTENSITY, 1.5f);
        if (!panel.unit.isDead) { AudioManager.instance.PlaySfx("damage02"); }

        Debug.Log("Stagger!");
        var popupText = Instantiate(popupPrefab, panel.transform, false);
        popupText.transform.localPosition += new Vector3(0f, 75f, 0f);
        popupText.DisplayMessage("Staggered!", shieldSprite, battleSpeed, Color.white, false);
        panel.isStunned = true;
        CalculateSpeedTicks(panel, 1f);
        StartCoroutine(DoFlashImage(panel.image, Color.red));
        StartCoroutine(DoPauseForStagger());
      }
      if (panel.unit.isDead) {
        AudioManager.instance.PlaySfx("cancel");
        FadeEnemyOut(panel);
      } else {
        panel.updateHpBar = true;
        UpdateCrystalsAndShields(panel);
      }
    }
    enemies = enemies.Where(e => !e.unit.isDead).ToList();
  }

  private void AddEffect(Panel panel, StatusEffect effect) {
    Debug.Log("Attempting to add effect: " + effect.effectName);
    if (effect.statusEffectType == StatusEffectTypes.Buff) {
      if (panel.buffs.effects.Any(e => e.effect?.effectName == effect.effectName)) { return; }
      var buff = Instantiate(buffPrefab, Vector3.zero, Quaternion.identity, panel.buffs.transform);
      Debug.Log("Adding buff to panel: " + effect.effectName);
      buff.icon.sprite = effect.sprite;
      buff.effect = effect;
      panel.buffs.effects.Add(buff);
    } else if (effect.statusEffectType == StatusEffectTypes.Debuff) {
        if (panel.debuffs.effects.Any(e => e.effect?.effectName == effect.effectName)) { return; }
      var debuff = Instantiate(buffPrefab, Vector3.zero, Quaternion.identity, panel.debuffs.transform);
      Debug.Log("Adding debuff to panel: " + effect.effectName);
      debuff.icon.sprite = effect.sprite;
      debuff.effect = effect;
      panel.debuffs.effects.Add(debuff);
    }
  }
  private void RemoveEffect(Panel panel, string effectName, StatusEffectTypes effectType) {
    Debug.Log("Calling Remove Effect for " + effectName);
    if (effectType == StatusEffectTypes.Buff) {
      foreach (var buff in panel.buffs.effects.ToList()) {
        if (!panel.buffs.effects.Any(e => e.effect.effectName == effectName)) continue;
        panel.buffs.effects.Remove(buff);
        Destroy(buff.gameObject);
        Debug.Log("Removing buff from panel: " + buff.effect.effectName);
      }
    } else if (effectType == StatusEffectTypes.Debuff) {
      foreach (var debuff in panel.debuffs.effects.ToList()) {
        if (!panel.debuffs.effects.Any(e => e.effect.effectName == effectName)) continue;
        panel.debuffs.effects.Remove(debuff);
        Destroy(debuff.gameObject);
        Debug.Log("Removing debuff from panel: " + debuff.effect.effectName);
      }
    }
  }

  private void FadeEnemyOut(EnemyPanel enemy) {
    enemy.FadeOut(battleSpeed / 2f);
  }

  private void SmoothHpBar(Panel panel, float newAmount) {
    // Debug.Log("Smoothing HP");
    if (panel.hpFillImage.fillAmount - newAmount > 0.001f) {
      panel.hpFillImage.fillAmount = Mathf.Lerp(panel.hpFillImage.fillAmount, newAmount, Time.deltaTime * (1 / battleSpeed) * 5f);
    } else {
      if (newAmount <= 0f) {
        panel.hpFillImage.fillAmount = 0f;
      }
      panel.updateHpBar = false;
    }
  }

  private void MovePanel(Panel panel, bool up) {
    panel.panelMoved = up;
    // Debug.Log("Starting pos: " + panel.transform.localPosition);
    var distance = 40f * (up ? 1f : -1f);
    panel.Move(distance, 0.25f * battleSpeed);
  }

  private void SlideHeroMenus(Hero hero, bool positive) {
    if (positive) { ShowHeroMenus(hero); }
    var endPos = actionMenu.transform.position + new Vector3(0f, (positive ? 160f : -160f), 0f);
    var startSize = shiftMenu.GetComponent<RectTransform>().sizeDelta;
    var endSize = (positive ? new Vector2(1250f, startSize.y) : shiftMenu.initialSize);
    StartCoroutine(DoMoveTo(actionMenu.transform, actionMenu.gameObject.transform.position, endPos, endSize, battleSpeed / 12f));
  }

  private IEnumerator DoMoveTo(Transform tform, Vector3 startPos, Vector3 endPos, Vector2 endSize, float time) {
    float elapsedTime = 0;
    // Debug.Log("Move to: " + startSize + " -> " + endSize);
    var startSize = shiftMenu.GetComponent<RectTransform>().sizeDelta;

    while (elapsedTime <= time) {
      tform.position = Vector3.Lerp(startPos, endPos, (elapsedTime / time));
      shiftMenu.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(startSize, endSize, (elapsedTime / time));
      elapsedTime += Time.deltaTime;
      yield return null;
    }
    // Make sure we got there
    tform.position = endPos;
    shiftMenu.GetComponent<RectTransform>().sizeDelta = endSize;
    yield return null;
  }

  private void ShowHeroMenus(Hero hero) {
    if (hero.shiftedThisTurn) {
      // another sound maybe
    } else {
      AudioManager.instance.PlaySfx("end_turn");
    }
    var mpModifier = 1f;
    if (currentPanel.buffs.effects.Any(b => b.effect.effectName == "Mana Song")) {
          mpModifier = 0.5f;
        }
    for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
      actionMenu.transform.position = actionMenu.initialPos;
      if (hero.currentJob.actions[i] != null) {
        SetupTooltip(actionMenu.actionButtons[i].tooltipButton, hero.currentJob.actions[i].name, "Battle Action".ToUpper(), hero.currentJob.actions[i].mpCost.ToString(), hero.currentJob.actions[i].description);
        actionMenu.actionButtons[i].gameObject.SetActive(true);
        actionMenu.actionButtons[i].icon.sprite = hero.currentJob.actions[i].sprite;
        actionMenu.actionButtons[i].fillColor.color = hero.currentJob.jobColor;
        if (hero.currentJob.actions[i].mpCost * mpModifier <= hero.mpCurrent) {
          actionMenu.actionButtons[i].tooltipButton.interactable = true;
        } else {
          actionMenu.actionButtons[i].tooltipButton.interactable = false;
        }
        actionMenu.actionButtons[i].nameText.text = hero.currentJob.actions[i].name;
        actionMenu.actionButtons[i].mpCost = hero.currentJob.actions[i].mpCost;
        var crystals = actionMenu.actionButtons[i].mpCost * mpModifier / 10;
        for (var m = 0; m < actionMenu.actionButtons[i].crystals.Length; m++) {
          if (m >= crystals) {
            actionMenu.actionButtons[i].crystals[m].gameObject.SetActive(false);
            continue;
          }
          actionMenu.actionButtons[i].crystals[m].gameObject.SetActive(true);
        }
      }
      else {
        actionMenu.actionButtons[i].gameObject.SetActive(false);
      }
    }

    // set current trait and shift action info
    SetupTooltip(shiftMenu.traitTooltipButton, hero.currentJob.trait.name, "Passive Trait".ToUpper(), "NA", hero.currentJob.trait.description);
    shiftMenu.traitColor.color = hero.currentJob.jobColor;
    shiftMenu.traitName.text = hero.currentJob.trait.name;
    shiftMenu.traitIcon.sprite = hero.currentJob.trait.sprite;
    SetupTooltip(shiftMenu.shiftActionTooltipButton, hero.currentJob.shiftAction.name, "Shift Action".ToUpper(), "NA", hero.currentJob.shiftAction.description);
    shiftMenu.shiftColor.color = hero.currentJob.jobColor;
    shiftMenu.shiftName.text = hero.currentJob.shiftAction.name;
    shiftMenu.shiftIcon.sprite = hero.currentJob.shiftAction.sprite;

    if (hero.jobs.Length > 1) {
      var jobIndex = System.Array.IndexOf(hero.jobs, hero.currentJob);
      var jobLIndex = (jobIndex - 1) < 0 ? hero.jobs.Length - 1 : jobIndex - 1;
      var jobRIndex = (jobIndex + 1) == hero.jobs.Length ? 0 : jobIndex + 1;
      var jobL = hero.jobs[jobLIndex];
      var jobR = hero.jobs[jobRIndex];
      shiftMenu.GetComponent<RectTransform>().sizeDelta = shiftMenu.initialSize;
      shiftMenu.gameObject.SetActive(true);
      shiftMenu.nameL.text = jobL.name;
      shiftMenu.jobIconL.sprite = jobL.jobIcon;
      shiftMenu.colorL.color = jobL.jobColor;
      shiftMenu.jobIdL = jobLIndex;
      SetupTooltip(shiftMenu.shiftLTooltipButton, jobL.name, "Job".ToUpper(), "NA", jobL.description, jobL);
      shiftMenu.nameR.text = jobR.name;
      shiftMenu.jobIconR.sprite = jobR.jobIcon;
      shiftMenu.colorR.color = jobR.jobColor;
      shiftMenu.jobIdR = jobRIndex;
      SetupTooltip(shiftMenu.shiftRTooltipButton, jobR.name, "Job".ToUpper(), "NA", jobR.description, jobR);
    }
    else {
      shiftMenu.gameObject.SetActive(false);
    }
    actionMenu.gameObject.SetActive(true);

  }

  private void SetupTooltip(TooltipButton button, string title, string kind, string cost, string content, Job job = null) {
    button.title = title;
    button.kind = kind;
    button.cost = cost;
    button.content = content;
    button.job = job;
  }

  private void ShakePanel(Panel panel, float intensity, float duration = 0.5f) {
    panel.shaker.Shake(intensity, duration * battleSpeed);
  }

  private void CalculateSpeedTicks(Panel panel, float delay) {
    var inverse = Mathf.Pow((1 + INITIATIVE_GROWTH), panel.unit.speed);
    var result = 1000 / inverse;
    result *= delay;
    panel.ticks = (int)result;
    // Debug.Log(combatant.name + " ticks: " + combatant.ticks);
  }

  // public void PredictTurnOrder() {
  //     while (potentialTurnOrder.Count < 5) {
  //         for (var i = 0; i < combatants.Count; i++) {
  //             if (combatants[i].ticks < 0){
  //                 Debug.LogError("Initiative went negative!");
  //                 combatants[i].ticks = 1;
  //             }
  //             if (combatants[i].isDead) {
  //                 potentialTurnOrder.RemoveAll(to => to == i);
  //                 continue;
  //             }
  //             combatants[i].ticks--;
  //             if (combatants[i].ticks == 0) {
  //                 potentialTurnOrder.Add(i);
  //                 // TODO: Replace with the actual speed of the action they use
  //                 combatants[i].ticks = CalculateSpeedTicks(combatants[i], 1f);
  //             }
  //         }
  //     }
  // }

  public void CountdownTicks() {
    while(true) {
      foreach(var combatant in combatants) {
        if (!combatant.unit.isDead && combatant.ticks == 0) {
          currentPanel = combatant;
          battleWaiting = true;
          return;
        } else {
          // Debug.Log(combatant.name + ": " + combatant.ticks);
          combatant.ticks--;
        }
      }
    }
  }

  public void ClickTarget(Panel target) {
    AudioManager.instance.PlaySfx("click");
    ClearAllTargetting();
    Debug.Log("Clicked target: " + target.unit.name);
    if (target.GetType() == typeof(EnemyPanel)) {
      playerTarget = target as EnemyPanel;
    } else {
      playerTarget = target as HeroPanel;
    }
    TargetChosen(currentAction);

    // } else if (playerTarget.unit.isDead) {
    //   playerTarget = combatants.Where(c => !c.unit.isPlayer && !c.unit.isDead).Cast<EnemyPanel>().First();

    // foreach (var panel in enemyPanels) {
    //   if (panel.unit == playerTarget) {
    //     panel.targetCursor.GetComponentInParent<Button>().Select();
    //   } else {
    //     panel.targetCursor.gameObject.SetActive(true);
    //   }
    // }
  }

  public void ClickActionButton(int buttonId) {
    if (!battleActive || delaying || targetingShiftAction) return;

    AudioManager.instance.PlaySfx("click");
    Action action = null;
    var hero = currentPanel.unit as Hero;
    if (currentAction != null) {
      if (currentAction.name == actionMenu.actionButtons[buttonId].nameText.text) {
        ClearAllTargetting();
        ResetActions();
        return;
      }
        for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
        if (actionMenu.actionButtons[i].nameText.text == currentAction.name) {
          actionMenu.actionButtons[i].GetComponent<Image>().color = black;
        }
      }
    }
    actionMenu.actionButtons[buttonId].GetComponent<Image>().color = yellow;
    var actionName = actionMenu.actionButtons[buttonId].nameText.text;
    foreach (var a in hero.currentJob.actions) {
      if (a.name == actionName) {
        action = a;
        break;
      }
    }
    if (action != null) {
      currentAction = action;
      Debug.Log("Current action: " + currentAction.name);

      if (action.targetType == TargetTypes.AllEnemies || action.targetType == TargetTypes.OneEnemy || action.targetType == TargetTypes.RandomEnemies) {
        ShowEnemyTargetting();
      } else if (action.targetType == TargetTypes.Self) {
        ShowSelfTargetting();
      } else if (action.targetType == TargetTypes.SelfOrAnAlly || action.targetType == TargetTypes.WholeParty) {
        ShowAllyTargetting();
      } else if (action.targetType == TargetTypes.OnlyAnAlly) {
        ShowAllyOnlyTargetting();
      }
    }
    else {
      Debug.Log("action is null!");
    }
  }

  private void ShowEnemyTargetting() {
    ClearAllTargetting();
    foreach(var panel in enemies) {
      panel.targetButton.interactable = true;
    }
    choosingTarget = true;
  }

  private void ShowSelfTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      if (panel == currentPanel) {
        panel.targetButton.interactable = true;
        break;
      }
    }
    choosingTarget = true;
  }

  private void ShowAllyTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      panel.targetButton.interactable = true;
    }
    choosingTarget = true;
  }

  private void ShowAllyOnlyTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      panel.targetButton.interactable = true;
    }
    choosingTarget = true;
  }

  private void ClearAllTargetting() {
    foreach(var panel in combatants) {
      panel.targetButton.interactable = false;
    }
    choosingTarget = false;
  }

  private void ResetActions() {
    if (currentAction == null) return;
    currentAction = null;
    for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
        actionMenu.actionButtons[i].GetComponent<Image>().color = black;
      }
  }

  public void TargetChosen(Action action) {
    HeroAction(action);
  }

  public void ClickShiftButton(int buttonId) {
    var hero = currentPanel.unit as Hero;
    var heroPanel = currentPanel as HeroPanel;
    if (hero.shiftedThisTurn) {
      var position = currentPanel.gameObject.transform.position;
      position.y += currentPanel.GetComponent<RectTransform>().rect.height;
      Instantiate(popupPrefab, position, currentPanel.gameObject.transform.rotation, currentPanel.transform).DisplayMessage("Already Shifted", null, battleSpeed * .8f, Color.white, false);
    } else {
      AudioManager.instance.PlaySfx("click");
      ClearAllTargetting();
      ResetActions();
      StartCoroutine(DoHandleStatusEffects(currentPanel, TriggerTypes.OnShift));
      shiftMenu.shiftLTooltipButton.interactable = false;
      shiftMenu.shiftRTooltipButton.interactable = false;
      StartCoroutine(DoShiftJobs(buttonId));
    }
  }

  public IEnumerator DoShiftJobs(int jobId) {
    var hero = currentPanel.unit as Hero;
    var heroPanel = currentPanel as HeroPanel;
    hero.shiftedThisTurn = true;
    SlideHeroMenus(hero, false);
    RemoveEffect(heroPanel, hero.currentJob.trait.name, StatusEffectTypes.Buff);
    hero.currentJob = hero.jobs[jobId];
    StartCoroutine(DoHandleTraits(heroPanel));
    yield return new WaitForSeconds(battleSpeed / 3f);

    SlideHeroMenus(hero, true);
    UpdateUi();
    HandleShiftAction();
  }

  public void HandleShiftAction() {
    var hero = currentPanel.unit as Hero;
    var action = hero.currentJob.shiftAction;
    shiftMenu.shiftActionTooltipButton.GetComponent<Image>().color = yellow;
    Tooltip.ShowTooltip(hero.currentJob.shiftAction.name, "Shift Action".ToUpper(), "NA", hero.currentJob.shiftAction.description);
    currentAction = action;
    targetingShiftAction = true;
    if (action != null && action.targetType == TargetTypes.OneEnemy || action.targetType == TargetTypes.OnlyAnAlly || action.targetType == TargetTypes.SelfOrAnAlly) {
      shiftMenu.selectTarget.gameObject.SetActive(true);
      Debug.Log("Current action: " + currentAction.name);

      if (action.targetType == TargetTypes.OneEnemy) {
        ShowEnemyTargetting();
      } else if (action.targetType == TargetTypes.OnlyAnAlly || action.targetType == TargetTypes.SelfOrAnAlly) {
        ShowAllyTargetting();
      }
    }
    else {
      TargetChosen(currentAction);
    }
  }

  public void HeroAction(Action actionToExecute) {
    var action = Instantiate(actionToExecute);
    Debug.Log("Executing: " + action.name);
    var nextTurn = true;
    if (targetingShiftAction) {
      nextTurn = false;
      shiftMenu.selectTarget.gameObject.SetActive(false);
      targetingShiftAction = false;
      Tooltip.HideTooltip();
    } else {
      SlideHeroMenus(currentPanel.unit as Hero, false);
    }
    StartCoroutine(DoHeroAction(action, null, null, nextTurn));
  }

  public IEnumerator DoHeroAction(Action action, HeroPanel heroPanel = null, EnemyPanel optionalTarget = null, bool nextTurn = true) {
    if (optionalTarget != null && optionalTarget.unit.isDead) yield break;
    var targetPanel = optionalTarget ?? playerTarget;
    var panel = heroes.Where(hp => hp == (heroPanel ?? currentPanel)).Single();
    if (action.mpCost > 0 && panel.buffs.effects.Any(b => b.effect.effectName == "Mana Song")) {
      Debug.Log("Mana Song working");
      panel.unit.mpCurrent -= action.mpCost / 2;
      // panel.buffsPanel.effects.RemoveAll(b => b.name == "Mana Song");
      RemoveEffect(panel, "Mana Song", StatusEffectTypes.Buff);
    } else {
      panel.unit.mpCurrent -= action.mpCost;
    }
    UpdateUi();

    var position = panel.gameObject.transform.position;
    position.y += panel.GetComponent<RectTransform>().rect.height * 0.8f;
    Instantiate(popupPrefab, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(action.name, action.sprite, battleSpeed * .8f, Color.white, false);
    yield return new WaitForSeconds(battleSpeed / 3f);

    if (!panel.isStaggered && action.damageType != DamageTypes.EffectOnly) {
      StartCoroutine(DoHandleStatusEffects(panel, TriggerTypes.OnAttack));
    }

    // Begin ACTION
    for (var h = 0; h < action.hits; h++) {
      while (pausedForStagger || delaying) {
        yield return new WaitForEndOfFrame();
      }

      // animations
      ExecuteActionFXs(action);

      if (action.targetType == TargetTypes.RandomEnemies) {
        var rand = Random.Range(0, enemies.Count - 1);
        var randomTarget = enemies[rand];
        HeroDealDamage(action, randomTarget, panel);
      } else {
        HandleActionTargetting(action, panel, targetPanel);
      }

      if (action.hits > 1) {
        yield return new WaitForSeconds(battleSpeed / 3f);
      }

      if (action.additionalAction.Count > 0) {
        foreach (var add in action.additionalAction) {
          yield return new WaitForSeconds(battleSpeed / 3f);
          Debug.Log("Additional damage" + add.name);
          if (add.targetType == TargetTypes.Self) {
            HeroHeal(add, panel);
          } else {
            HeroDealDamage(add, targetPanel, panel);
          }
        }
      }
    }

    if (action.damageType == DamageTypes.EffectOnly && action.buffs.Count > 0) {
      foreach(var buff in action.buffs) {
        if (buff.targetType == TargetTypes.Self) {
          HeroApplyBuff(buff, panel);
        } else if (buff.targetType == TargetTypes.SelfOrAnAlly || buff.targetType == TargetTypes.OnlyAnAlly ) {
          HeroApplyBuff(buff, playerTarget);
        } else if (buff.targetType == TargetTypes.WholeParty) {
          foreach (var hero in heroes) {
            HeroApplyBuff(buff, hero);
          }
        } else if (buff.targetType == TargetTypes.BothAllies) {
          foreach(var hero in heroes.Where(h => h.unit.name != panel.unit.name)) {
            HeroApplyBuff(buff, hero);
          }  
        }
      }
    }

    if (action.damageType != DamageTypes.EffectOnly) {
      StartCoroutine(DoHandleStatusEffects(panel, TriggerTypes.AfterAttack));
      if (action.targetType == TargetTypes.AllEnemies) {
        foreach(var enemy in enemies) {
          StartCoroutine(DoHandleStatusEffects(enemy, TriggerTypes.AfterBeingAttacked));
        }
      } else {
        if (playerTarget != null && playerTarget.GetType() == typeof(EnemyPanel)) {
          StartCoroutine(DoHandleStatusEffects(playerTarget, TriggerTypes.AfterBeingAttacked));
        }
      }
    }

    if (nextTurn) {
      CalculateSpeedTicks(currentPanel, action.delay);
      if (panel.panelMoved) {
        MovePanel(panel, false);
      }
      Debug.Log("DoDelay");
      StartCoroutine(DoDelay());
      currentPanel.dmgIncreasePercentMod = 0f;
      currentPanel.dmgIncreaseFlatMod = 0;
      NextTurn();
    } else {
      shiftMenu.shiftActionTooltipButton.GetComponent<Image>().color = black;
      // shiftMenu.traitTooltipButton.GetComponent<Image>().color = black;
    }
  }

  private void HandleActionTargetting(Action action, HeroPanel heroPanel = null, Panel targetPanel = null) {
    if (playerTarget != null && playerTarget.unit.isDead) {
      return;
    }
    if (targetPanel != null && targetPanel.unit.isDead) {
      return;
    }
    switch (action.targetType) {
      case TargetTypes.OneEnemy:
        var target = enemies.Where(ep => ep == (targetPanel ?? playerTarget)).Single();
        HeroDealDamage(action, target, heroPanel);
        break;
      case TargetTypes.AllEnemies:
        foreach(var panel in enemies) {
          HeroDealDamage(action, panel, heroPanel);
          if (action.additionalAction.Count > 0) {
            foreach (var add in action.additionalAction) {
              HeroDealDamage(add, panel, heroPanel);
            }
          }
        }
        break;
      case TargetTypes.Self:
      case TargetTypes.SelfOrAnAlly:
      case TargetTypes.OnlyAnAlly:
        HeroHeal(action, playerTarget as HeroPanel);
        break;
      case TargetTypes.BothAllies:
        var allies = heroes.Where(h => h != currentPanel);
        foreach(var panel in allies.Where(a => a.unit.name != currentPanel.unit.name)) {
          HeroHeal(action, panel);
        }
        break;
      case TargetTypes.WholeParty:
        foreach(var panel in heroes) {
          HeroHeal(action, panel);
        }
        break;
      default:
        Debug.LogError("Invalid action type! " + action.targetType);
        break;
    }
    
    if (action.damageType != DamageTypes.EffectOnly && action.buffs.Count > 0) {
      foreach(var buff in action.buffs) {
        if (!currentPanel.buffs.effects.Any(b => b.effect.effectName == buff.effectName)) {
          Debug.Log("Added buff: " + buff.effectName);
          // currentPanel.buffs.Add(buff);
          AddEffect(currentPanel, buff);
        }
      }
    }
    
    if (action.damageType == DamageTypes.Martial) {
      var takeAim = currentPanel.buffs.effects.Where(b => b.effect.effectName == "Take Aim").Select(b => b.effect).FirstOrDefault();
      if (takeAim != null) {
        // currentPanel.buffs.Remove(takeAim);
        RemoveEffect(currentPanel, takeAim.effectName, takeAim.statusEffectType);
      }
    }
    if (action.name == "Shatter") {
      Debug.Log("Shatter over. " + currentPanel.unit.name);
      currentPanel.unit.armorCurrent = 0;
      UpdateUi();
    }
  }

  public void HeroHeal(Action action, HeroPanel targetPanel, HeroPanel userPanel = null) {
    var user = userPanel ?? currentPanel;
    var isCrit = false;
    var color = Color.white;

    var power = 0f;

    if (action.powerType == PowerTypes.Attack) {
      power = user.unit.attack;
    }
    else if (action.powerType == PowerTypes.Willpower) {
      power = user.unit.willpower;
    }

    var amount = power;
    var message = "";
    Sprite sprite = action.sprite;
    
    if (action.damageType == DamageTypes.ArmorGain) {
      color = gray;
      sprite = shieldSprite;
      Debug.Log(sprite.name);
      if (targetPanel.isStaggered) {
        amount = 0;
      } else {
        if (action.name == "Mage Armor") {
          amount = Mathf.Clamp(action.potency - targetPanel.unit.armorCurrent, 0, action.potency);
          Debug.Log("potency: " + action.potency + " Armor: " + targetPanel.unit.armorCurrent);
        } else {
          amount = action.potency;
        }
        targetPanel.unit.armorCurrent = Mathf.Clamp(targetPanel.unit.armorCurrent + (int)amount, 0, 100);
        amount /= 10;
      }
    } else if (action.damageType == DamageTypes.ManaGain) {
      color = blue;
      sprite = crystal;
      amount = action.potency;
      targetPanel.unit.mpCurrent = Mathf.Clamp(targetPanel.unit.mpCurrent + (int)amount, 0, 100);
      amount /= 10;
    } else if (action.damageType == DamageTypes.HealthGain) {
      color = green;
      sprite = heart;
      targetPanel.unit.hpCurrent = Mathf.Clamp(targetPanel.unit.hpCurrent + (int)amount, 0, targetPanel.unit.hpMax);
      amount *= action.potency;
    } else if (action.damageType == DamageTypes.EffectOnly) {
      message = action.name;
      // Action: GAMBIT
      if (action.name == "Gambit") {
        Debug.Log("Gambit");
        var swap = targetPanel.unit.mpCurrent;
        targetPanel.unit.mpCurrent = targetPanel.unit.armorCurrent;
        targetPanel.unit.armorCurrent = swap;
      }
    }

    var panel = heroes.Where(hp => hp == targetPanel).Single();
    var position = panel.image.transform.position;

    if (action.damageType != DamageTypes.EffectOnly) {
      message = amount.ToString("N0");
      Instantiate(damagePrefab, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(message, sprite, battleSpeed * 0.8f, color, isCrit, true);
    }
    UpdateUi();
  }

  public void HeroDealDamage(Action action, Panel defender, Panel hero = null) {
    var attacker = hero ?? currentPanel;
    var isCrit = false;
    var color = Color.white;
    var damageType = action.damageType;
    var sprite = action.sprite;

    if (damageType == DamageTypes.Martial) {
      var isTakingAim = currentPanel.buffs?.effects?.Any(b => b.effect.effectName == "Take Aim");
      if (isTakingAim == true) {
        damageType = DamageTypes.Piercing;
      }
    }
    // check for Crit
    var critRoll = Random.Range(1, 100);
    if (critRoll <= attacker.unit.crit) {
      isCrit = true;
    }

    var damage = CalculateDamage(attacker, defender, action, damageType, isCrit);
    var message = damage;

    
    if (damageType == DamageTypes.Martial) {
      color = orange;
    } else if (damageType == DamageTypes.Ether) {
      color = purple;
    } else if (damageType == DamageTypes.Piercing) {
      color = yellow;
    } else if (damageType == DamageTypes.ArmorGain) {
      color = gray;
      sprite = shieldSprite;
    } 
    
    if (action.damageType != DamageTypes.EffectOnly) {
      var position = defender.image.transform.position;
      Instantiate(damagePrefab, position, defender.gameObject.transform.rotation, canvas.transform).DisplayMessage(message, sprite, battleSpeed * 0.8f, color, isCrit, true);
      ShakePanel(defender, SHAKE_INTENSITY);
    }

    if (action.debuffs.Count > 0) {
      Debug.Log("Going to apply debuff for : " + action.name );
      foreach(var debuff in action.debuffs) {
        HeroApplyDebuff(debuff, defender, hero as HeroPanel);
      }
    }

    UpdateUi();
  }

  public void HeroApplyBuff(StatusEffect effect, Panel target, HeroPanel user = null) {
    AddEffect(target, effect);
    var color = Color.white;
    var position = target.image.transform.position;
    Instantiate(popupPrefab, position, target.gameObject.transform.rotation, canvas.transform).DisplayMessage(effect.effectName, effect.sprite, battleSpeed * 0.5f, color, false, true);
  }

  public void HeroApplyDebuff(StatusEffect effect, Panel defender, HeroPanel user = null) {
    AddEffect(defender, effect);
    if (effect.fadesOnCasterTrigger) {
      var hero = user ?? currentPanel as HeroPanel;
      hero.debuffsOnOthers.Add(effect);
      Debug.Log("Added to others! :)");
    }
    var color = Color.white;
    var position = defender.image.transform.position;
    Instantiate(popupPrefab, position, defender.gameObject.transform.rotation, canvas.transform).DisplayMessage(effect.effectName, effect.sprite, battleSpeed * 0.5f, color, false, true);
    ShakePanel(defender, SHAKE_INTENSITY);
  }

  public void ExecuteActionFXs(Action action) {
    AudioManager.instance.PlaySfx(action.sfxName);
  }

  public void EnemyAction() {
    StartCoroutine(DoFlashImage(currentPanel.image, Color.clear));

    var enemy = currentPanel.unit as Enemy;

    var rand = Random.Range(0, enemy.actions.Count);

    var action = enemy.actions[rand];

    // Debug.Log("Executing: " + action.name);
    StartCoroutine(DoEnemyAction(action));
  }

  private IEnumerator DoEnemyAction(Action action) {
    yield return new WaitForSeconds(battleSpeed / 5f);
    var position = currentPanel.gameObject.transform.localPosition;
    // Debug.Log("Enemy pos: " + position);
    position.y += currentPanel.GetComponent<RectTransform>().rect.height * 2;

    var popupText = Instantiate(popupPrefab, currentPanel.transform, false);
    popupText.transform.localPosition += new Vector3(0f, 75f, 0f);
    popupText.DisplayMessage(action.name, action.sprite, battleSpeed * 0.8f, Color.white, false);

    currentPanel.unit.mpCurrent -= action.mpCost;
    UpdateUi();
    yield return new WaitForSeconds(battleSpeed / 2f);

    StartCoroutine(DoHandleStatusEffects(enemyTarget, TriggerTypes.OnBeingAttacked));
    if (action.targetType == TargetTypes.OneEnemy) {
      for (var h = 0; h < action.hits; h++) {
        while (pausedForStagger || delaying || pausedForTriggers) {
          yield return new WaitForEndOfFrame();
        }
        ExecuteActionFXs(action);
        EnemyDealDamage(action);
        if (action.additionalAction.Count > 0) {
          foreach (var add in action.additionalAction) {
            yield return new WaitForSeconds(battleSpeed / 3f);
            Debug.Log("Additional enemy action " + add.name);
            if (add.targetType == TargetTypes.Self) {
              Debug.Log("Add self additional damage actions");
            } else {
              EnemyDealDamage(add);
            }
          }
        }
        if (action.hits > 1) {
          yield return new WaitForSeconds(battleSpeed / 4f);
        }
      }
    } else if (action.targetType == TargetTypes.AllEnemies) {
      foreach(var heroPanel in heroes) {
        for (var h = 0; h < action.hits; h++){
          while (pausedForStagger || delaying || pausedForTriggers) {
            yield return new WaitForEndOfFrame();
          }
          ExecuteActionFXs(action);
          EnemyDealDamage(action, heroPanel);

          if (action.additionalAction.Count > 0) {
            foreach (var add in action.additionalAction) {
              yield return new WaitForSeconds(battleSpeed / 3f);
              Debug.Log("Additional damage" + add.name);
              if (add.targetType == TargetTypes.Self) {
                Debug.Log("Add self additional damage actions");
              } else {
                EnemyDealDamage(add);
              }
            }
          }
          if (action.hits > 1) {
            yield return new WaitForSeconds(battleSpeed / 4f);
          }
        }
      }
    }

    yield return new WaitForSeconds(battleSpeed);

    while (pausedForTriggers) {
      yield return new WaitForEndOfFrame();
    }
    CalculateSpeedTicks(currentPanel, action.delay);
    Debug.Log("Enemy turn done.");
    NextTurn();
  }

  private void EnemyDealDamage(Action action, HeroPanel targetPanel = null) {
    // Debug.Log("Enemy dealing damage to " + enemyTarget.heroName);
    var attacker = currentPanel;
    var defender = targetPanel ?? enemyTarget;
    var color = Color.white;
    var isCrit = false;
    var damageType = action.damageType;
    var sprite = action.sprite;
    var dmgReduction = defender.dmgReductionPercentMod;

    // check for Crit
    var critRoll = Random.Range(1, 100);
    // Debug.Log("Crit Roll: " + critRoll + "/" + attacker.crit);
    if (critRoll <= attacker.unit.crit) {
      isCrit = true;
    }

    if (action.damageType == DamageTypes.Martial) {
      color = orange;
    } else if (action.damageType == DamageTypes.Ether) {
      color = purple;
    } else if (action.damageType == DamageTypes.Piercing) {
      color = yellow;
    } else if (damageType == DamageTypes.ArmorGain) {
      color = gray;
      sprite = shieldSprite;
      isCrit = false;
    }  else if (damageType == DamageTypes.ManaGain) {
      color = gray;
      sprite = crystal;
      isCrit = false;
    } 

    var damage = CalculateDamage(attacker, defender, action, damageType, isCrit);

    var panel = heroes.Where(hp => hp == defender).Single();
    var position = panel.gameObject.transform.position + new Vector3(0f, 50f, 0f);
    Instantiate(damagePrefab, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage, sprite, battleSpeed * 0.8f, color, isCrit, true);
    ShakePanel(panel, SHAKE_INTENSITY);

    if (!defender.isStaggered && damageType == DamageTypes.Martial) {
      StartCoroutine(DoHandleStatusEffects(defender, TriggerTypes.OnBeingHit));
    }

    UpdateUi();
  }

  private string CalculateDamage(Panel attacker, Panel defender, Action action, DamageTypes damageType, bool isCrit) {
    var power = 0f;

    if (action.powerType == PowerTypes.Attack) {
      power = attacker.unit.attack;
    }
    else if (action.powerType == PowerTypes.Willpower) {
      power = attacker.unit.willpower;
    } else if (action.powerType == PowerTypes.None) {
      return "";
    }
    else {
      Debug.LogError("Missing power type!");
    }

    var damage = power;
    var potency = action.potency;

    if (isCrit) {
      damage += attacker.unit.surge;
    }
    
    // TODO: Fix
    if (action.name == "Mana Bolt") {
      potency *= defender.unit.mpCurrent;
      Debug.Log("Mana Bolt potency: " + potency);
    } else if (action.name == "Disintegrate") {
      potency *= (1 + (1 - defender.unit.hpPercent));
    } else if (action.name == "Shatter") {
      potency *= attacker.unit.armorCurrent;
      Debug.Log("Shatter potency: " + potency);
    }

    if (action.splitDamage) {
      potency /= enemies.Where(e => !e.unit.isDead).Count();
    }

    damage *= potency;
    damage += currentPanel.dmgIncreaseFlatMod;
    
    var defense = 0f;

    if (!defender.isStaggered && action.removesArmor) {
      defender.unit.armorCurrent -= 10;
      if (defender.unit.armorCurrent <= -10) {
        if (action.name == "Potshot") {
          Debug.Log("Adding mana gain");
          action.additionalAction.Add(Instantiate(Resources.Load<Action>("Actions/Rogue/" + "Potshot Managain")));
        }
        defender.unit.armorCurrent = -10;
      }
    }

    if (damageType == DamageTypes.Martial) {
      defense = (defender.isStaggered ? 0 : defender.unit.Defense);
    }
    else if (damageType == DamageTypes.Ether) {
      defense = (defender.isStaggered ? 0 : defender.unit.Resist);
    }

    damage *= (1f - defense);

    if (defender.isStaggered) {
      damage *= attacker.unit.breakBonus;
    }

    damage *= (1 + attacker.dmgIncreasePercentMod - defender.dmgReductionPercentMod);

    if (attacker.dmgIncreasePercentMod > 0 || defender.dmgReductionPercentMod > 0) {
      Debug.Log("Damage altered! Increase: " + attacker.dmgIncreasePercentMod + " reduction: " + defender.dmgReductionPercentMod);
    }

    if (attacker.dmgIncreasePercentMod > 0) {
      Debug.Log("Damage increased by " + attacker.dmgIncreasePercentMod + "% = " + damage);
    }

    if (damageType == DamageTypes.ArmorGain) {
      // Action: Dissonance
      if (action.name == "Dissonance") {
        if (!defender.isStaggered) {
          damage = (float)Mathf.CeilToInt(defender.unit.armorCurrent / 20) * 10;
        } else {
          damage = 0;
        }
      }
      defender.unit.armorCurrent -= (int)damage;
      damage /= 10f;
    } if (damageType == DamageTypes.ManaGain) {
      damage = action.potency;
      defender.unit.mpCurrent = (int)Mathf.Clamp(defender.unit.mpCurrent - (int)damage, 0, 10);
      damage /= 10f;
    } else {
      defender.unit.hpCurrent -= (int)damage;
      if (defender.unit.hpCurrent < 0) {
        defender.unit.hpCurrent = 0;
      }
      // Debug.Log(defender.name + "'s HP: " + defender.hpCurrent);
    }

    // Debug.Log(attacker.unit.name + " -> " + defender.unit.name + " for " + damage + " * (1 - " + defense + ") = " + (int)(damage * (1f - defense)) + " potency: " + potency);

    var result = ((int)damage).ToString("N0");
    if (isCrit) {
      result += "!";
    } if (defender.isStaggered) {
      result += "!!";
    }
    UpdateUi();
    return result;
  }

  private IEnumerator DoHandleStatusEffects(Panel panel, TriggerTypes trigger) {
    // Debug.Log(panel.unit.name + " triggering: " + trigger.ToString());
    var buffs = panel.buffs?.effects?.Where(b => b.effect?.activationTrigger == trigger).Select(b => b.effect);
    var debuffs = panel.debuffs?.effects?.Where(b => b.effect?.activationTrigger == trigger).Select(b => b.effect);
    var effects = new List<StatusEffect>();
    if (buffs != null && buffs.Any()) { effects.AddRange(buffs); }
    if (debuffs != null && debuffs.Any()) { effects.AddRange(debuffs); }
    if (effects.Count > 0) {
      pausedForTriggers = true;
      for(var i = 0; i < effects.Count; i++) {
      Debug.Log(panel.unit.name + " Effect name: " + effects[i].effectName + " " + effects.Count);
        while (pausedForStagger) {
          yield return new WaitForEndOfFrame();
        }
        // Begin Checking Effects
        if (trigger == TriggerTypes.OnAttack) {
          if (effects[i].effectName == "War Cry") {
            Debug.Log("Triggering War Cry!");
            panel.dmgIncreasePercentMod += .5f;
          }
        }

        if (trigger == TriggerTypes.OnBeingAttacked) {
          if (effects[i].effectName == "Protected") {
            Debug.Log("Triggering Protected!");
            panel.dmgReductionPercentMod += .15f;
          }
        }

        if (trigger == TriggerTypes.OnBeingHit) {
          if (effects[i].effectName == "Counterattack") {
            yield return new WaitForSeconds(battleSpeed * 0.5f);
            var action = Instantiate(Resources.Load<Action>("Actions/Knight/" + "Counterattack"));
            Debug.Log("Counterattack!");
            if (effects.Any(e => e.effectName == "Riposte")) {
              Debug.Log("Riposte!");
              action.additionalAction.Add(Instantiate(Resources.Load<Action>("Actions/Knight/" + "RiposteCounter")));
            }
            StartCoroutine(DoHeroAction(action, panel as HeroPanel, currentPanel as EnemyPanel, false));
          }
        }

        else if (trigger == TriggerTypes.StartOfTurn) {
          if (panel.GetType() == typeof(HeroPanel)){
            var hero = panel.unit as Hero;
            if (hero.currentJob.trait.name == effects[i].effectName) {
              shiftMenu.traitTooltipButton.GetComponent<Image>().color = yellow;
            }
          }
          // KNIGHT
          if (effects[i].effectName == "Healing Aura") {
            var action = Instantiate(Resources.Load<Action>("Actions/Knight/" + effects[i].effectName));
            StartCoroutine(DoHeroAction(action, panel as HeroPanel, null, false));
            yield return new WaitForSeconds(battleSpeed * 0.5f);
          }

          if (effects[i].effectName == "Arcane Nova") {
            var action = Instantiate(Resources.Load<Action>("Actions/Mage/" + effects[i].effectName));
            StartCoroutine(DoHeroAction(action, panel as HeroPanel, null, false));
            yield return new WaitForSeconds(battleSpeed * 0.5f);
          }
          if (effects[i].effectName == "Mirror Images") {
            var action = Instantiate(Resources.Load<Action>("Actions/Mage/" + effects[i].effectName));
            StartCoroutine(DoHeroAction(action, panel as HeroPanel, null, false));
            yield return new WaitForSeconds(battleSpeed * 0.5f);
          }
        }

        // Done Checking Effects

        yield return new WaitForSeconds(battleSpeed * 0.5f);
        if (i == effects.Count - 1) {
          pausedForTriggers = false;
        }
      }
    }

    // panel.buffs.RemoveAll(b => b.fadeTrigger == trigger);
    // panel.debuffs.RemoveAll(d => d.fadeTrigger == trigger);
    // panel.buffsOnOthers.RemoveAll(b => b.fadeTrigger == trigger);
    var debuffsToRemove = panel.debuffsOnOthers.Where(d => d.fadeTrigger == trigger);
    foreach (var enemy in enemyPanels) {
      foreach(var db in debuffsToRemove) {
        Debug.Log("Debuff removing: " + db.effectName);
        // enemy.debuffs.RemoveAll(d => d.name == db.name);
      }
    }
    // panel.debuffsOnOthers.RemoveAll(d => d.fadeTrigger == trigger);
    yield break;
  }
  
  private IEnumerator DoHandleTraits(HeroPanel heroPanel) {
    // Remove fading effects
    var hero = heroPanel.unit as Hero;
    var trait = hero.currentJob.trait;
    pausedForTriggers = true;
    while (pausedForStagger) {
      yield return new WaitForEndOfFrame();
    }

    // KNIGHT
    if (trait.name == "Counterattack") {
      var buff = Instantiate(Resources.Load<StatusEffect>("Actions/StatusEffects/" + trait.name));
      // heroPanel.buffs.Add(buff);
      HeroApplyBuff(buff, heroPanel);
      yield return new WaitForSeconds(battleSpeed * 0.5f);
    }
    if (trait.name == "Protection Aura") {
      Debug.Log("Protection Aura activating! NYI");
    }
    // MAGE
    if (trait.name == "Arcane Nova") {
      var buff = Instantiate(Resources.Load<StatusEffect>("Actions/StatusEffects/" + trait.name));
      AddEffect(heroPanel, buff);
      yield return new WaitForSeconds(battleSpeed * 0.5f);
    }
    if (trait.name == "Mirror Images") {
      var buff = Instantiate(Resources.Load<StatusEffect>("Actions/StatusEffects/" + trait.name));
      AddEffect(heroPanel, buff);
      yield return new WaitForSeconds(battleSpeed * 0.5f);
    }
    // ROGUE
    if (trait.name == "Mesmerize") {
      Debug.Log("Mesmerize activating! NYI");
    }

    yield return new WaitForSeconds(battleSpeed * 0.5f);
    pausedForTriggers = false;
    if (trait.traitType == TraitTypes.Passive) {
      Debug.Log("Yellow Trait " + trait.name);
      shiftMenu.traitTooltipButton.GetComponent<Image>().color = yellow;
    } else {
      Debug.Log("Black Trait " + trait.name);
      shiftMenu.traitTooltipButton.GetComponent<Image>().color = black;
    }
    yield break;
  }

  private IEnumerator DoFlashImage(Image image, Color color) {
    var originalColor = image.color;
    for (int n = 0; n < 2; n++) {
      image.color = color;
      yield return new WaitForSeconds(.05f);
      image.color = originalColor;
      yield return new WaitForSeconds(.05f);
    }
  }

  private IEnumerator DoDelay(float duration = 1f) {
    delaying = true;
    duration *= battleSpeed;
    yield return new WaitForSeconds(duration);
    delaying = false;
  }

  private IEnumerator DoPauseForStagger(float duration = 1f) {
    pausedForStagger = true;
    duration *= battleSpeed;
    yield return new WaitForSeconds(duration);
    pausedForStagger = false;
  }
}
