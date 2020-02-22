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
  public List<PredictedPanel> potentialTurnOrder;
  public Panel currentPanel;
  public Panel playerTarget;
  public HeroPanel enemyTarget;
  public Action currentAction;

  [Header("Colors & Icons")]
  public Texture2D cursorTexture;
  public Texture2D targetTexture;
  public Vector2 hotSpot = Vector2.zero;
  public CursorMode cursorMode = CursorMode.Auto;
  public Sprite heart;
  public Sprite crystal, shieldSprite;
  public Color orange, purple, yellow, gray, black, blue, green;

  const float INITIATIVE_GROWTH = .05f;
  const float SHAKE_INTENSITY = 20f;

  void Awake() {
    instance = this;
    DontDestroyOnLoad(gameObject);
    Screen.orientation = ScreenOrientation.Landscape;
    Screen.SetResolution(1920, 1080, true, 60);
  }
  void Start() {
    actionMenu.gameObject.SetActive(false);
    foreach(var panel in enemyPanels) {
      panel.staggerIcon.gameObject.SetActive(false);
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
      heroPanels[i].staggerIcon.gameObject.SetActive(false);
      if (i >= GameManager.instance.heroes.Count) { 
        heroPanels[i].gameObject.SetActive(false);
        heroPanels[i].GetComponent<RectTransform>().gameObject.SetActive(false);
        continue;
      } else {
        var panel = heroPanels[i];
        panel.unit = GameManager.instance.heroes[i];
        panel.gameObject.SetActive(true);
        panel.GetComponent<RectTransform>().gameObject.SetActive(true);
        panel.Setup();
      }
    }

    for(var i = 0; i < enemyPanels.Length; i++) {
      if (i >= enemyLoadList.Count) { 
        enemyPanels[i].gameObject.SetActive(false);
        continue;
      } else {
        var panel = enemyPanels[i] as EnemyPanel;
        panel.unit = Instantiate(enemyLoadList[i]);
        panel.gameObject.SetActive(true);
        panel.Setup();

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

  void Update() {
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
    if (Input.GetKeyUp(KeyCode.Escape)) {
      Application.Quit();
    } else if (Input.GetKeyUp(KeyCode.X)) {
      heroes[0].unit.hpCurrent = (int)((float)heroes[0].unit.hpCurrent * 0.8f);
      UpdateUi(heroes[0]);
    }
  }

  void SetInitialTicks() {
      foreach (var c in combatants) {
        var rand = (decimal)Random.Range(0.5f, 1.5f);
        c.ticks = CalculateSpeedTicks(c, rand);
      }
  }

  void FixedUpdate() {
    if (!battleActive || !battleWaiting || delaying || pausedForStagger || pausedForTriggers || choosingTarget) { return; }

    battleWaiting = false;

    // VICTORY!
    if (enemies.All(x => x.isDead)) return;

    if (currentPanel.buffs != null) {
      currentPanel.buffs.TriggerEffects(TriggerTypes.StartOfTurn);
    }

    currentPanel.unit.mpCurrent += currentPanel.unit.mpRegen;
    UpdateUi(currentPanel);

    if (currentPanel.exposed) {
      currentPanel.remainingStaggeredTurns--;
      Debug.Log($"Updating stagger text for { currentPanel.name } from { currentPanel.staggerIcon.text.text } to { currentPanel.remainingStaggeredTurns.ToString() }.");
      currentPanel.staggerIcon.text.text = currentPanel.remainingStaggeredTurns.ToString();
      if (!currentPanel.exposed) {
        currentPanel.staggerIcon.gameObject.SetActive(false);
        UpdateUi(currentPanel);
      } else {
        currentPanel.ticks = CalculateSpeedTicks(currentPanel, 1m);
        NextTurn();
        return;
      }
    }

    if (currentPanel.isStunned) {
      currentPanel.ticks = CalculateSpeedTicks(currentPanel, 1m);
      currentPanel.isStunned = false;
      NextTurn();
      return;
    }

    if (currentPanel.unit.isPlayer) {
      if (currentPanel.unit.isDead) {
        currentPanel.ticks = 1000;
      }
      // HERO TURN
      var hero = currentPanel.unit as Hero;
      hero.shiftedThisTurn = false;
      shiftMenu.shiftLTooltipButton.interactable = true;
      shiftMenu.shiftRTooltipButton.interactable = true;
      MovePanel(currentPanel, true);
      SlideActionMenus(hero, true);
      SlideShiftMenus(hero, true);

    } else {
      // ENEMY TURN
      var targetId = 0;
      var taunters = heroes.Where(h => h.taunting && !h.isDead).ToList();
      if (taunters.Count > 0) {
        targetId = Random.Range(0, taunters.Count);
        enemyTarget = taunters[targetId];
        Debug.Log($"Number of taunters: { taunters.Count }. Targetting: { enemyTarget.name }");
      } else {
        // Target Randomly
        targetId = Random.Range(0, heroes.Where(h => !h.isDead).Count());
        enemyTarget = heroes[targetId];
      }
      EnemyAction();
    }
  }

  void NextTurn() {
    ResetActions();
    playerTarget = null;
    actionMenu.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    // check for battle end
    if (enemies.All(e => e.isDead)) {
      Debug.Log("You win!");
    }

    CountdownTicks();
    // GetPotentialTurnOrder();
  }

  void UpdateUi(Panel panel) {
    if (panel.unit.armorCurrent < 0 && !panel.exposed && !panel.isDead) {
      // Exposed!
      panel.remainingStaggeredTurns = panel.staggerDelayAmount;
      panel.staggerIcon.text.text = panel.remainingStaggeredTurns.ToString();
      panel.staggerIcon.gameObject.SetActive(true);
      ShakePanel(panel, SHAKE_INTENSITY, 1.5f);
      AudioManager.instance.PlaySfx("damage02");
      Debug.Log($"{ panel.name } was staggered! Ticks reset from { panel.ticks } -> { CalculateSpeedTicks(panel, 1m) }.");
      panel.ticks = CalculateSpeedTicks(panel, 1m);
      StartCoroutine(DoFlashImage(panel.image, orange));
      StartCoroutine(DoPauseForStagger());
    }
    panel.updateHpBar = true;
    panel.UpdateCrystalsAndShields();
    if (panel.GetType() == typeof(HeroPanel)) {
      var hero = panel.unit as Hero;
      panel.currentHp.text = panel.unit.hpCurrent.ToString("N0");
      var heroPanel = panel as HeroPanel;
      heroPanel.currentJobName.text = hero.currentJob.name.ToUpper();
      heroPanel.jobColor.color = hero.currentJob.jobColor;
      heroPanel.jobIcon.sprite = hero.currentJob.jobIcon;
    } else {
      if (panel.isDead) {
        AudioManager.instance.PlaySfx("cancel");
        FadeEnemyOut(panel as EnemyPanel);
        enemies = enemies.Where(e => !e.isDead).ToList();
      }
    }
    return;
  }

  void UpdateUi() {
    for (var i = 0; i < heroes.Count; i++) {
      UpdateUi(heroes[i]);
    }
    for(var i = 0; i < enemies.Count; i++) {
      UpdateUi(enemies[i]);
    }
    enemies = enemies.Where(e => !e.isDead).ToList();
  }

  void FadeEnemyOut(EnemyPanel enemy) {
    enemy.FadeOut(battleSpeed / 2f);
  }

  void SmoothHpBar(Panel panel, float newAmount) {
    if (Mathf.Abs(panel.hpFillImage.fillAmount - newAmount) > 0.001f) {
      panel.hpFillImage.fillAmount = Mathf.Lerp(panel.hpFillImage.fillAmount, newAmount, Time.deltaTime * (1 / battleSpeed) * 5f);
    } else {
      if (newAmount <= 0f) {
        panel.hpFillImage.fillAmount = 0f;
      }
      panel.updateHpBar = false;
    }
  }

  void MovePanel(Panel panel, bool up) {
    panel.panelMoved = up;
    var distance = 40f * (up ? 1f : -1f);
    panel.Move(distance, 0.25f * battleSpeed);
  }

  void SlideActionMenus(Hero hero, bool positive) {
    if (positive) { ShowHeroMenus(hero); }
    var endPos = actionMenu.transform.position + new Vector3(0f, (positive ? 1.8f : -1.8f), 0f);
    StartCoroutine(DoMoveTo(actionMenu.transform, actionMenu.gameObject.transform.position, endPos, battleSpeed / 12f));
  }

  void SlideShiftMenus(Hero hero, bool positive) {
    var startSize = shiftMenu.GetComponent<RectTransform>().sizeDelta;
    var endSize = (positive ? new Vector2(1900f, startSize.y) : shiftMenu.initialSize);
    StartCoroutine(DoResizeTo(endSize, battleSpeed / 12f));
  }

  IEnumerator DoMoveTo(Transform tform, Vector3 startPos, Vector3 endPos, float time) {
    float elapsedTime = 0;

    while (elapsedTime <= time) {
      tform.position = Vector3.Lerp(startPos, endPos, (elapsedTime / time));
      elapsedTime += Time.deltaTime;
      yield return null;
    }
    // Make sure we got there
    tform.position = endPos;
    yield return null;
  }

  IEnumerator DoResizeTo(Vector2 endSize, float time) {
    float elapsedTime = 0;
    var startSize = shiftMenu.GetComponent<RectTransform>().sizeDelta;

    while (elapsedTime <= time) {
      shiftMenu.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(startSize, endSize, (elapsedTime / time));
      elapsedTime += Time.deltaTime;
      yield return null;
    }
    // Make sure we got there
    shiftMenu.GetComponent<RectTransform>().sizeDelta = endSize;
    yield return null;
  }

  void ShowHeroMenus(Hero hero) {
    if (!hero.shiftedThisTurn) {
      // another sound maybe
      AudioManager.instance.PlaySfx("end_turn");
    }

    if (currentPanel.buffs.currentDisplays.Any(b => b.effect.name == "Mana Song")) {
      currentPanel.unit.mpModifier = -0.5f;
    }

    actionMenu.transform.position = actionMenu.initialPos;

    actionMenu.DisplayActionMenu(hero);
    shiftMenu.DisplayShiftMenu(hero);

    actionMenu.gameObject.SetActive(true);
    shiftMenu.gameObject.SetActive(true);
  }

  void ShakePanel(Panel panel, float intensity, float duration = 0.5f) {
    panel.shaker.Shake(intensity, duration * battleSpeed);
  }

  decimal CalculateSpeedTicks(Panel panel, decimal delay) {
    var inverse = (decimal)Mathf.Pow((1 + INITIATIVE_GROWTH), panel.unit.speed);
    var result = 100m / inverse;
    result *= delay;
    result *= (decimal)(1 + panel.speedMod);
    return result;
    // Debug.Log(combatant.name + " ticks: " + combatant.ticks);
  }

  // TODO: FIx this broken shit
  // void GetPotentialTurnOrder() {
  //   potentialTurnOrder = new List<PredictedPanel>();
  //   foreach(var combatant in combatants.Where(c => !c.isDead).OrderBy(c => c.ticks)) {
  //     var refCombatant = combatant;
  //     var entry = new PredictedPanel(0m, ref refCombatant);
  //     potentialTurnOrder.Add(entry);
  //   }
  //   while (potentialTurnOrder.Count < 10) {
  //     var nextTickDown = potentialTurnOrder.OrderBy(c => c.ticks).First().ticks;
  //     foreach(var pto in potentialTurnOrder) {
  //       var refCombatant = pto.panel;
  //       var predictedPanel = new PredictedPanel(CalculateSpeedTicks(refCombatant, 1m), ref refCombatant);
  //       predictedPanel.ticks -= nextTickDown;
  //       if (pto.ticks <= 0) {
  //         potentialTurnOrder.Add(new PredictedPanel(0, ref refCombatant));
  //         return;
  //       } else {
  //         pto.ticks -= nextTickDown;
  //       }
  //     }
  //   }
  // }

  public void CountdownTicks() {
    while(true) {
      var livingCombatants = combatants.Where(c => !c.isDead);
      var nextTickDown = livingCombatants.OrderBy(c => c.ticks).First().ticks;
      foreach(var combatant in livingCombatants) {
        // Debug.Log(combatant.name + ": " + combatant.ticks.ToString("#.###") + " - nextTickDown: " + nextTickDown.ToString("#.###"));
        combatant.ticks -= nextTickDown;
        if (combatant.ticks <= 0) {
          combatant.ticks = 0;
          currentPanel = combatant;
        }
      }
      // Debug.Log("Current combatant: " + currentPanel.name + " ticks: " + currentPanel.ticks);
      battleWaiting = true;
      return;
    }
  }

  public void ClickTarget(Panel target) {
    AudioManager.instance.PlaySfx("click");
    ClearAllTargetting();
    Debug.Log("Clicked target: " + target.name);
    if (target.GetType() == typeof(EnemyPanel)) {
      playerTarget = target as EnemyPanel;
    } else {
      playerTarget = target;
    }
    TargetChosen(currentAction);

    // } else if (playerTarget.isDead) {
    //   playerTarget = combatants.Where(c => !c.unit.isPlayer && !c.isDead).Cast<EnemyPanel>().First();

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
        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
        ClearAllTargetting();
        ResetActions();
        return;
      }
        for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
        if (actionMenu.actionButtons[i].nameText.text == currentAction.name) {
          actionMenu.actionButtons[i].GetComponentInChildren<PulsingImage>().StopPulse();
          actionMenu.actionButtons[i].GetComponent<Image>().color = black;
        }
      }
    }
    actionMenu.actionButtons[buttonId].GetComponent<Image>().color = yellow;
    actionMenu.actionButtons[buttonId].GetComponentInChildren<PulsingImage>().Pulse(battleSpeed * 0.5f);
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

      if (action.targetType == TargetTypes.Self || action.targetType == TargetTypes.AllEnemies || action.targetType == TargetTypes.RandomEnemies || action.targetType == TargetTypes.WholeParty) {
        ClearAllTargetting();
        ResetActions();
        HeroAction(action);
        return;
      }

      // Tooltip.ShowTooltip(action.name, "Battle Action".ToUpper(), action.mpCost.ToString(), action.description);
      Debug.Log("Showing tooltip");

      Cursor.SetCursor(targetTexture, hotSpot, cursorMode);

      if (action.targetType == TargetTypes.OneEnemy) {
        ShowEnemyTargetting();
      } else if (action.targetType == TargetTypes.SelfOrAnAlly) {
        ShowAllyTargetting();
      } else if (action.targetType == TargetTypes.OnlyAnAlly) {
        Debug.Log("Ally only!");
        ShowAllyOnlyTargetting();
      }
    }
    else {
      Debug.Log("action is null!");
    }
  }

  void ShowEnemyTargetting() {
    ClearAllTargetting();
    foreach(var panel in enemies) {
      panel.targetButton.interactable = true;
    }
    choosingTarget = true;
  }

  void ShowSelfTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      if (panel == currentPanel) {
        panel.targetButton.interactable = true;
        break;
      }
    }
    choosingTarget = true;
  }

  void ShowAllyTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      panel.targetButton.interactable = true;
    }
    choosingTarget = true;
  }

  void ShowAllyOnlyTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes.Where(h => h.name != currentPanel.name)) {
      panel.targetButton.interactable = true;
    }
    choosingTarget = true;
  }

  void ClearAllTargetting() {
    foreach(var panel in combatants) {
      panel.targetButton.interactable = false;
    }
    choosingTarget = false;
  }

  void ResetActions() {
    if (currentAction is null) return;
    currentAction = null;
    for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
      actionMenu.actionButtons[i].GetComponentInChildren<PulsingImage>().StopPulse();
      actionMenu.actionButtons[i].GetComponent<Image>().color = black;
    }
  }

  public void TargetChosen(Action action) {
    HeroAction(action);
  }

  public void ClickShiftButton(int buttonId) {
    var hero = currentPanel.unit as Hero;
    if (!hero.shiftedThisTurn) {
      AudioManager.instance.PlaySfx("click");
      ClearAllTargetting();
      ResetActions();
      Debug.Log("Shifting - checking status effects");
      shiftMenu.shiftLTooltipButton.interactable = false;
      shiftMenu.shiftRTooltipButton.interactable = false;
      StartCoroutine(DoHandleStatusEffects(currentPanel, TriggerTypes.OnShift));
      StartCoroutine(DoShiftJobs(buttonId));
    }
  }

  public IEnumerator DoShiftJobs(int jobId) {
    var hero = currentPanel.unit as Hero;
    var heroPanel = currentPanel;
    hero.shiftedThisTurn = true;
    SlideActionMenus(hero, false);
    SlideShiftMenus(hero, false);
    Debug.Log("Shift buff removal");

    if (hero.currentJob.trait.name == "Mesmerize") {
      Debug.Log("Mesmerize fading!");
      var debuff = Instantiate(Resources.Load<StatusEffect>("Actions/StatusEffects/" + hero.currentJob.trait.name));
      foreach(var enemy in enemies) {
        Debug.Log(enemy.name + " current ticks: " + enemy.ticks.ToString("#.###") + " speed mod: " + enemy.speedMod + "%");
        enemy.ticks *= (decimal)(1 - enemy.speedMod);
        enemy.speedMod -= debuff.speedMod;
        Debug.Log(enemy.name + " new ticks: " + enemy.ticks.ToString("#.###") + " speed mod: " + enemy.speedMod + "%");
      }
    } else if (hero.currentJob.trait.name == "Drums of War") {
      var buff = Instantiate(Resources.Load<StatusEffect>("Actions/StatusEffects/" + hero.currentJob.trait.name));
      foreach(var h in heroes) {
        Debug.Log(h.name + " current ticks: " + h.ticks.ToString("#.###") + " speed mod: " + h.speedMod);
        h.ticks *= (decimal)(1 + h.speedMod);
        h.speedMod += buff.speedMod;
        Debug.Log(h.name + " new ticks: " + h.ticks.ToString("#.###") + " speed mod: " + h.speedMod);
      }
    } else if (hero.currentJob.trait.name == "Protection Aura") {
      foreach(var h in heroes) {
        h.damageTakenPercentMod = +0.15f;
      }
    }
    currentPanel.buffs.RemoveEffect(hero.currentJob.trait.name);
    // RemoveEffect(heroPanel, hero.currentJob.trait.name, StatusEffectTypes.Buff);
    hero.currentJob = hero.jobs[jobId];
    StartCoroutine(DoHandleTraits(heroPanel));
    yield return new WaitForSeconds(battleSpeed / 3f);

    UpdateUi();
    HandleShiftAction();
  }

  public void HandleShiftAction() {
    var hero = currentPanel.unit as Hero;
    var action = hero.currentJob.shiftAction;
    shiftMenu.shiftActionTooltipButton.interactable = true;
    Tooltip.ShowTooltip(hero.currentJob.shiftAction.name, "Shift Action".ToUpper(), "NA", hero.currentJob.shiftAction.description);
    currentAction = action;
    targetingShiftAction = true;
    SlideShiftMenus(hero, true);
    if (action != null && action.targetType == TargetTypes.OneEnemy || action.targetType == TargetTypes.OnlyAnAlly || action.targetType == TargetTypes.SelfOrAnAlly) {
      Cursor.SetCursor(targetTexture, hotSpot, cursorMode);
      ShowHeroMenus(hero);
      shiftMenu.selectTarget.gameObject.SetActive(true);
      shiftMenu.selectTarget.GetComponent<PulsingText>().Pulse(battleSpeed);
      // Debug.Log("Current action: " + currentAction.name);

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
    Tooltip.HideTooltip();
    var action = Instantiate(actionToExecute);
    Debug.Log("Executing: " + action.name);
    var nextTurn = true;
    if (targetingShiftAction) {
      nextTurn = false;
      shiftMenu.selectTarget.gameObject.SetActive(false);
      targetingShiftAction = false;
      Tooltip.HideTooltip();
      SlideActionMenus(currentPanel.unit as Hero, true);
    } else {
      SlideActionMenus(currentPanel.unit as Hero, false);
      SlideShiftMenus(currentPanel.unit as Hero, false);

    }
    Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
    StartCoroutine(DoHeroAction(action, null, null, nextTurn));
  }

  public IEnumerator DoHeroAction(Action action, Panel heroPanel = null, Panel optionalTarget = null, bool nextTurn = true) {
    if (optionalTarget != null && optionalTarget.isDead) yield break; // optional target died

    var target = optionalTarget ?? playerTarget ?? currentPanel;
    
    var actor = heroes.Where(hp => hp == (heroPanel ?? currentPanel)).Single();

    var manaSong = actor.buffs.currentDisplays.Where(b => b.effect.name == "Mana Song").FirstOrDefault();
    if (action.mpCost > 0 && manaSong != null) {
      Debug.Log($"{ manaSong.name } working");
      actor.unit.mpCurrent -= action.mpCost / 2;
      actor.unit.mpModifier = 0f;
      actor.buffs.RemoveEffect(manaSong.name);
    } else {
      actor.unit.mpCurrent -= action.mpCost;
    }

    UpdateUi(actor);

    var position = actor.gameObject.transform.position;
    position.y += 0.3f;
    Instantiate(popupPrefab, position, actor.gameObject.transform.rotation, canvas.transform).DisplayMessage(action.name, action.sprite, battleSpeed * .8f, Color.white, false);
    yield return new WaitForSeconds(battleSpeed / 2f);

    if (!actor.exposed && action.damageType != DamageTypes.EffectOnly) {
      StartCoroutine(DoHandleStatusEffects(actor, TriggerTypes.OnAttack));
    }

    // Begin ACTION
    for (var h = 0; h < action.hits; h++) {
      while (pausedForStagger || delaying) {
        yield return new WaitForEndOfFrame();
      }

      // animations
      ExecuteActionFXs(action, target);

      HandleActionTargetting(action, actor, target);

      if (action.hits > 1) {
        yield return new WaitForSeconds(battleSpeed / 3f);
      }

      if (action.additionalActions.Count > 0) {
        pausedForTriggers = true;
        foreach (var add in action.additionalActions) {
          yield return new WaitForSeconds(battleSpeed * 0.2f);
          Instantiate(popupPrefab, position, actor.gameObject.transform.rotation, canvas.transform).DisplayMessage(add.name, add.sprite, battleSpeed * .8f, Color.white, false);
          yield return new WaitForSeconds(battleSpeed / 2f);
          Debug.Log($"Action { action.name } is dealing additional damage from { add.name } of type { add.damageType }.");
          if (add.targetType == TargetTypes.Self) {
            HeroHeal(add, actor);
          } else if (add.targetType == TargetTypes.WholeParty) {
            foreach(var hero in heroes) {
              HeroHeal(add, hero);
            }
          }
           else {
            HeroDealDamage(add, target, actor);
          }
        }
        yield return new WaitForSeconds(battleSpeed / 2f);
        pausedForTriggers = false;
      }
    }

    if (action.damageType == DamageTypes.EffectOnly && action.buffs.Count > 0) {
      foreach(var buff in action.buffs) {
        if (buff.targetType == TargetTypes.Self) {
          HeroApplyBuff(buff, actor);
        } else if (buff.targetType == TargetTypes.SelfOrAnAlly || buff.targetType == TargetTypes.OnlyAnAlly ) {
          HeroApplyBuff(buff, playerTarget);
        } else if (buff.targetType == TargetTypes.WholeParty) {
          foreach (var hero in heroes) {
            HeroApplyBuff(buff, hero);
          }
        } else if (buff.targetType == TargetTypes.BothAllies) {
          foreach(var hero in heroes.Where(h => h.name != actor.name)) {
            HeroApplyBuff(buff, hero);
          }  
        }
      }
    }

    if (action.damageType != DamageTypes.EffectOnly) {
      if (action.damageType == DamageTypes.Martial || action.damageType == DamageTypes.Ether || action.damageType == DamageTypes.Piercing)
      // Debug.Log("after attacking");
      StartCoroutine(DoHandleStatusEffects(actor, TriggerTypes.AfterAttacking));
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

    var takeAim = currentPanel.buffs.currentDisplays.Where(b => b.effect.name == "Take Aim").Select(b => b.effect).FirstOrDefault();
    if (takeAim != null && takeAim.readyToFade) {
      currentPanel.buffs.RemoveEffect("Take Aim");
    }

    if (nextTurn) {
      currentPanel.ticks += CalculateSpeedTicks(currentPanel, (decimal)action.delay);
      if (actor.panelMoved) {
        MovePanel(actor, false);
      }
      // Debug.Log("DoDelay");
      StartCoroutine(DoDelay());
      NextTurn();
    } else {
      shiftMenu.shiftActionTooltipButton.interactable = false;
      Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
    }
  }

  void HandleActionTargetting(Action action, HeroPanel heroPanel = null, Panel targetPanel = null) {
    if ((playerTarget != null && playerTarget.isDead) || (targetPanel != null && targetPanel.isDead)) { return; }

    switch (action.targetType) {
      case TargetTypes.RandomEnemies:
        var rand = Random.Range(0, enemies.Count - 1);
        var randomTarget = enemies[rand];
        HeroDealDamage(action, randomTarget, heroPanel);
        break;
      case TargetTypes.OneEnemy:
        var target = enemies.Where(ep => ep == (targetPanel ?? playerTarget)).Single();
        HeroDealDamage(action, target, heroPanel);
        break;
      case TargetTypes.AllEnemies:
        foreach(var panel in enemies) {
          HeroDealDamage(action, panel, heroPanel);
          if (action.additionalActions.Count > 0) {
            foreach (var add in action.additionalActions) {
              HeroDealDamage(add, panel, heroPanel);
            }
          }
        }
        break;
      case TargetTypes.Self:
        HeroHeal(action, currentPanel);
        break;
      case TargetTypes.SelfOrAnAlly:
      case TargetTypes.OnlyAnAlly:
        HeroHeal(action, playerTarget);
        break;
      case TargetTypes.BothAllies:
        var allies = heroes.Where(h => h != currentPanel);
        foreach(var panel in allies.Where(a => a.name != currentPanel.name)) {
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
        var buffPanel = currentPanel;
        if (buff.targetType != TargetTypes.Self) {
          buffPanel = playerTarget;
        }
        buffPanel.buffs.AddEffect(buff);
      }
    }
    
    if (action.damageType == DamageTypes.Martial) {
      var takeAim = currentPanel.buffs.currentDisplays.Where(b => b.effect.name == "Take Aim").Select(b => b.effect).FirstOrDefault();
      if (takeAim != null) {
        takeAim.readyToFade = true;
      }
    }
    if (action.name == "Shatter") {
      Debug.Log("Shatter over. " + currentPanel.name);
      currentPanel.unit.armorCurrent = 0;
      UpdateUi();
    }
  }

  public void HeroHeal(Action action, Panel targetPanel, Panel userPanel = null) {
    if (targetPanel != null) {
      Debug.Log("Target to heal is: " + targetPanel.name);
    }
    var user = userPanel ?? currentPanel;
    var target = targetPanel ?? playerTarget;
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
      if (target.exposed) {
        amount = 0;
      } else {
        if (action.name == "Mage Armor") {
          amount = Mathf.Clamp(action.potency - target.unit.armorCurrent, 0, action.potency);
          Debug.Log("potency: " + action.potency + " Armor: " + target.unit.armorCurrent);
        } else {
          amount = action.potency;
        }
        target.unit.armorCurrent = Mathf.Clamp(target.unit.armorCurrent + (int)amount, 0, 200);
        amount /= 10;
      }
    } else if (action.damageType == DamageTypes.ManaGain) {
      color = blue;
      sprite = crystal;
      amount = action.potency;
      target.unit.mpCurrent = Mathf.Clamp(target.unit.mpCurrent + (int)amount, 0, 100);
      Debug.Log($"{ target.name }'s MP increased by { (int)amount } -> Mana: { target.unit.mpCurrent }.");
      amount /= 10;
      if (action.name == "Battle Orders") {
        var boost = CalculateSpeedTicks(target, 0.3m);
        Debug.Log("Battle Orders!! " + target.name + " was boosted by " + boost.ToString("#.###") + " ticks. Ticks: " + target.ticks.ToString("#.###") + " -> " + (target.ticks - boost).ToString("#.###"));
        target.ticks -= boost;
      }
      if (currentPanel == target) {
        actionMenu.DisplayActionMenu(currentPanel.unit as Hero);
      }
    } else if (action.damageType == DamageTypes.HealthGain) {
      color = green;
      sprite = heart;
      amount *= action.potency;
      if (action.name == "Healing Aura") {
        amount += (target.unit.hpMax - target.unit.hpCurrent) * 0.33f;
      } else if (action.name == "Mend Wounds") {
        amount *= (3 - (target.unit.hpPercent));
      }
      target.unit.hpCurrent = Mathf.Clamp(target.unit.hpCurrent + (int)amount, 0, target.unit.hpMax);
    } else if (action.damageType == DamageTypes.TurnDelay) {
      var boost = CalculateSpeedTicks(target, (decimal)action.potency);
      Debug.Log($"{ action.name } used -> { target.name } was boosted by { boost.ToString("#.###") } ticks. Ticks: { target.ticks.ToString("#.###") } -> { (target.ticks - boost).ToString("#.###") } ");
      target.ticks -= boost;
    } else if (action.damageType == DamageTypes.EffectOnly) {
      message = action.name;
      // Action: GAMBIT
      if (action.name == "Gambit") {
        Debug.Log("Gambit");
        var swap = target.unit.mpCurrent;
        target.unit.mpCurrent = target.unit.armorCurrent;
        target.unit.armorCurrent = swap;
      }
    }

    var panel = heroes.Where(hp => hp == (target ?? user)).SingleOrDefault();
    if (panel is null) {
      Debug.LogError( $"{ user.name }'s { action.name } is messed up! It has no target");
    }
    var position = panel.transform.position;

    if (action.damageType != DamageTypes.EffectOnly) {
      message = amount.ToString("N0");
      if (amount > 0 || action.damageType == DamageTypes.HealthGain) {
        Instantiate(damagePrefab, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(message, sprite, battleSpeed * 0.8f, color, isCrit, true);
      }
    }
    UpdateUi();
  }

  public void HeroDealDamage(Action action, Panel defender, Panel hero = null) {
    var attacker = hero ?? currentPanel;
    var isCrit = false;
    var color = Color.white;
    var damageType = action.damageType;
    var sprite = action.sprite;
    var critChance = attacker.unit.crit;

    // check for Crit

    if (defender.debuffs.currentDisplays.Any(e => e.effect.name == "Hunter's Mark")) {
      Debug.Log("Hunter's Mark is working");
      critChance += 25;
    }
    if (damageType == DamageTypes.Martial) {
      var isTakingAim = currentPanel.buffs?.currentDisplays?.Any(b => b.effect.name == "Take Aim");
      if (isTakingAim == true) {
        critChance += 50;
      }
    }
    var critRoll = Random.Range(1, 100);
    if (critRoll <= critChance) {
      isCrit = true;
    }

    var damage = CalculateDamage(attacker, defender, action, damageType, isCrit);
    var message = damage.ToString();

    if (isCrit) {
      message += "!";
    } if (defender.exposed || defender.unit.armorCurrent < 0) {
      color = orange;
    }
    
    if (damageType == DamageTypes.Piercing) {
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

    if (action.drainPotency > 0) {
      var drainAmount = (int)(damage * action.drainPotency);
      attacker.unit.hpCurrent = Mathf.Clamp(attacker.unit.hpCurrent + drainAmount, 0, attacker.unit.hpMax);
      Debug.Log("Draining " + drainAmount + " HP!");
      var position = attacker.image.transform.position;
      message = drainAmount.ToString("N0");
      Instantiate(damagePrefab, position, attacker.gameObject.transform.rotation, canvas.transform).DisplayMessage(message, sprite, battleSpeed * 0.8f, green, false, true);
    }

    if (defender.debuffs.currentDisplays.Where(e => e.gameObject.activeInHierarchy).Any(e => e.name == "Confusion")) {
      var delay = CalculateSpeedTicks(defender, 0.05m);
      Debug.Log("Confusion delay!! " + defender.name + " was delayed by " + delay.ToString("#.###") + " ticks. Ticks: " + defender.ticks.ToString("#.###") + " -> " + (defender.ticks + delay).ToString("#.###"));
      defender.ticks += delay;
    }

    if (action.debuffs.Count > 0) {
      Debug.Log("Going to apply debuff for : " + action.name );
      foreach(var debuff in action.debuffs) {
        HeroApplyDebuff(debuff, defender, hero);
      }
    }

    UpdateUi();
  }

  public void HeroApplyBuff(StatusEffect effect, Panel target, Panel user = null, bool displayMessage = true) {
    target.buffs.AddEffect(effect);
    var color = Color.white;
    var position = target.image.transform.position;
    position.y += 0.3f;
    if (displayMessage) {
      Instantiate(popupPrefab, position, target.gameObject.transform.rotation, canvas.transform).DisplayMessage(effect.name, effect.sprite, battleSpeed * 0.5f, color, false, true);
    }
  }

  public void HeroApplyDebuff(StatusEffect effect, Panel defender, Panel user = null) {
    defender.debuffs.AddEffect(effect);
    var color = Color.white;
    var position = defender.image.transform.position;
    position.y += 0.3f;
    Instantiate(popupPrefab, position, defender.gameObject.transform.rotation, canvas.transform).DisplayMessage(effect.name, effect.sprite, battleSpeed * 0.5f, color, false, true);
    ShakePanel(defender, SHAKE_INTENSITY / 5f);
  }

  public void ExecuteActionFXs(Action action, Panel target) {
    AudioManager.instance.PlaySfx(action.sfxName);
    // if (action.particleFx != null && target != null) {
    //   Debug.Log("Executing particle fx");
    //   var position = currentPanel.transform.position;
    //   Instantiate(action.particleFx, position, target.transform.rotation, canvas.transform);
    // }
  }

  public void EnemyAction() {
    StartCoroutine(DoFlashImage(currentPanel.image, Color.clear));

    var enemy = currentPanel.unit as Enemy;
    var possibleActions = enemy.actions.Where(a => a.mpCost <= enemy.mpCurrent).ToList();
    var rand = Random.Range(0, possibleActions.Count);
    var action = possibleActions[rand];

    // Debug.Log("Executing: " + action.name);
    StartCoroutine(DoEnemyAction(action));
  }

  IEnumerator DoEnemyAction(Action action) {
    var popupText = Instantiate(popupPrefab, currentPanel.transform, false);
    popupText.transform.position += new Vector3(0f, 0.3f, 0f);
    popupText.DisplayMessage(action.name, action.sprite, battleSpeed * 0.8f, Color.white, false);

    currentPanel.unit.mpCurrent -= action.mpCost;
    UpdateUi(currentPanel);
    yield return new WaitForSeconds(battleSpeed / 4f);

    // StartCoroutine(DoHandleStatusEffects(enemyTarget, TriggerTypes.OnBeingAttacked));

    if (enemyTarget.buffs.currentDisplays.Count > 0) {
      for (var i = 0; i < enemyTarget.buffs.currentDisplays.Count; i++) {
        if (enemyTarget.buffs.currentDisplays[i].effect.activationTrigger == TriggerTypes.OnBeingAttacked) {
          Debug.Log("Attacked triggers!");
        }
      }
    }
    if (action.targetType == TargetTypes.OneEnemy) {
      for (var h = 0; h < action.hits; h++) {
        while (pausedForStagger || delaying || pausedForTriggers) {
          yield return new WaitForEndOfFrame();
        }
        ExecuteActionFXs(action, enemyTarget);
        EnemyDealDamage(action);
        if (action.additionalActions.Count > 0) {
          foreach (var add in action.additionalActions) {
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
          ExecuteActionFXs(action, enemyTarget);
          EnemyDealDamage(action, heroPanel);

          if (action.additionalActions.Count > 0) {
            foreach (var add in action.additionalActions) {
              yield return new WaitForSeconds(battleSpeed / 2f);
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

    while (pausedForStagger || delaying || pausedForTriggers) {
      yield return new WaitForEndOfFrame();
    }
    currentPanel.ticks += CalculateSpeedTicks(currentPanel, (decimal)action.delay);
    Debug.Log("Enemy turn done. " + currentPanel.name + ": " + currentPanel.ticks.ToString("#.###"));
    NextTurn();
  }

  void EnemyDealDamage(Action action, HeroPanel targetPanel = null) {
    // Debug.Log("Enemy dealing damage to " + enemyTarget.heroName);
    var attacker = currentPanel;
    var defender = targetPanel ?? enemyTarget;
    var color = Color.white;
    var isCrit = false;
    var damageType = action.damageType;
    var sprite = action.sprite;
    var dmgReduction = defender.damageTakenPercentMod;

    // check for Crit
    var critRoll = Random.Range(1, 100);
    // Debug.Log("Crit Roll: " + critRoll + "/" + attacker.crit);
    if (critRoll <= attacker.unit.crit) {
      isCrit = true;
    }

    var damage = CalculateDamage(attacker, defender, action, damageType, isCrit);
    var message = damage.ToString("N0");

    if (isCrit) {
      message += "!";
    } if (defender.exposed || defender.unit.armorCurrent < 0) {
      color = orange;
    }
    
    if (damageType == DamageTypes.Piercing) {
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
    
    if (action.drainPotency > 0) {
      var drainAmount = (int)(damage * action.drainPotency);
      attacker.unit.hpCurrent = Mathf.Clamp(attacker.unit.hpCurrent + drainAmount, 0, attacker.unit.hpMax);
      Debug.Log("Draining " + drainAmount + " HP!");
      var enemyPosition = attacker.image.transform.position;
      var drainMessage = drainAmount.ToString("N0");
      Instantiate(damagePrefab, enemyPosition, attacker.gameObject.transform.rotation, canvas.transform).DisplayMessage(message, sprite, battleSpeed * 0.8f, green, false, true);
    }

    var panel = heroes.Where(hp => hp == defender).Single();
    var position = panel.gameObject.transform.position;
    Instantiate(damagePrefab, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(message, sprite, battleSpeed * 0.8f, color, isCrit, true);
    ShakePanel(panel, SHAKE_INTENSITY);

    if (!defender.exposed && defender.unit.armorCurrent > 0) {
      StartCoroutine(DoHandleStatusEffects(defender, TriggerTypes.OnBeingHit));
    }
    UpdateUi();
  }

  int CalculateDamage(Panel attacker, Panel defender, Action action, DamageTypes damageType, bool isCrit) {
    var power = 0f;

    if (action.powerType == PowerTypes.Attack) {
      power = attacker.unit.attack;
    }
    else if (action.powerType == PowerTypes.Willpower) {
      power = attacker.unit.willpower;
    } else if (action.powerType == PowerTypes.None) {
      return 0;
    }
    else {
      Debug.LogError("Missing power type!");
    }

    var damage = power;
    var potency = action.potency;

    if (isCrit) {
      damage += attacker.unit.critBonus;
    }
    
    if (action.name == "Mana Bolt") {
      potency *= defender.unit.mpCurrent;
      Debug.Log("Mana Bolt potency: " + potency);
    } else if (action.name == "Disintegrate") {
      potency *= (1 + (1 - defender.unit.hpPercent));
    } else if (action.name == "Shatter") {
      potency *= attacker.unit.armorCurrent;
      Debug.Log("Shatter potency: " + potency);
    } if (action.name == "Overload") {
      potency *= (100 + attacker.unit.mpCurrent) / 100f;
      Debug.Log("Overload potency: " + potency);
    }

    if (action.splitDamage) {
      potency /= enemies.Where(e => !e.isDead).Count();
    }

    damage *= potency;
    damage += currentPanel.damageDealtFlatMod;
    
    var defense = 0f;

    if (!defender.exposed && action.removesArmor) {
      defender.unit.armorCurrent -= 10;
    }
      if (action.name == "Potshot") {
        if (defender.unit.armorCurrent < 0 || defender.exposed) {
          Debug.Log("Adding mana gain");
          action.additionalActions.Add(Instantiate(Resources.Load<Action>("Actions/Rogue/" + "Potshot Managain")));
        } else if (action.additionalActions.Count > 0) {
          action.additionalActions.Clear();
        }
      }

    if (damageType == DamageTypes.Martial) {
      defense = ((defender.unit.armorCurrent < 0 || defender.exposed) ? 0 : defender.unit.Defense);
      // Debug.Log("defense: " + defense);
    }
    else if (damageType == DamageTypes.Ether) {
      defense = ((defender.unit.armorCurrent < 0 || defender.exposed) ? 0 : defender.unit.Resist);
    }

    if (damageType == DamageTypes.Martial) {
      var isTakingAim = currentPanel.buffs?.currentDisplays?.Any(b => b.effect.name == "Take Aim");
      if (isTakingAim == true) {
        defense = 0f;
      }
    }

    damage *= (1f - defense);
    var bonus = damage * attacker.unit.breakBonus;
    if (defender.unit.armorCurrent < 0 || defender.exposed) {
      damage = bonus;
    }

    if (damageType == DamageTypes.Martial) {
      var isTakingAim = currentPanel.buffs?.currentDisplays?.Any(b => b.effect.name == "Take Aim");
      if (isTakingAim == true) {
        damage = bonus;
      }
    }

    damage *= (1 + attacker.damageDealtPercentMod - defender.damageTakenPercentMod);

    if (attacker.damageDealtPercentMod != 0) {
      Debug.Log("Damage increased by " + (attacker.damageDealtPercentMod * 100) + "% = " + damage);
    }

    if (damageType == DamageTypes.ArmorGain) {
      // Action: Dissonance
      if (action.name == "Dissonance") {
        if (!defender.exposed) {
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

    // Debug.Log(attacker.name + " -> " + defender.name + " for " + damage + " * (1 - " + defense + ") = " + (int)(damage * (1f - defense)) + " potency: " + potency);

    UpdateUi();
    return Mathf.Clamp((int)damage, 1, (int)damage);
  }

  IEnumerator DoHandleStatusEffects(Panel panel, TriggerTypes trigger) {
    var buffs = panel.buffs.currentDisplays;
    var debuffs = panel.debuffs.currentDisplays;
    var debuffsOnOthers = panel.debuffsOnOthers;
    if (buffs.Count == 0 && debuffs.Count == 0 && debuffsOnOthers.Count == 0) { yield break; }
    pausedForTriggers = true;

    panel.buffs.TriggerEffects(trigger);
    panel.debuffs.TriggerEffects(trigger);
    
    for (var i = 0; i < debuffsOnOthers.Count; i++) {
      if (debuffsOnOthers[i].effect.fadeTrigger == trigger) {
        Debug.Log("removing effect on other: " + debuffsOnOthers[i].effect.name);
        debuffsOnOthers[i].gameObject.SetActive(false);
      }
    }

    pausedForTriggers = false;
    yield break;
  }
  
  IEnumerator DoHandleTraits(Panel panel) {
    var hero = panel.unit as Hero;
    var trait = Instantiate(hero.currentJob.trait);
    while (pausedForStagger) {
      yield return new WaitForEndOfFrame();
    }

    panel.buffs.AddEffect(trait);
    Debug.Log($"{ trait.name } is being applied to { panel.name }!");

    // Trait effects

    // KNIGHT
    if (trait.name == "Protection Aura") {
      foreach(var h in heroes) {
        h.damageTakenPercentMod = -0.15f;
      }
    }
    
    else if (trait.name == "Drums of War") {
      foreach(var h in heroes) {
        // Debug.Log(h.name + " current ticks: " + h.ticks.ToString("#.###") + " speed mod: " + h.speedMod);
        h.speedMod -= trait.speedMod;
        h.ticks *= (decimal)(1 + h.speedMod);
        // Debug.Log(h.name + " new ticks: " + h.ticks.ToString("#.###") + " speed mod: " + h.speedMod);
      }
    }

    // MAGE


    // ROGUE
    if (trait.name == "Mesmerize") {
      foreach(var enemy in enemies) {
        // Debug.Log(enemy.name + " current ticks: " + enemy.ticks.ToString("#.###") + " speed mod: " + enemy.speedMod);
        enemy.speedMod += trait.speedMod;
        enemy.ticks *= (decimal)(1 + enemy.speedMod);
        // Debug.Log(enemy.name + " new ticks: " + enemy.ticks.ToString("#.###") + " speed mod: " + enemy.speedMod);
      }
    }

    yield return new WaitForSeconds(battleSpeed * 0.5f);
  }

  IEnumerator DoFlashImage(Image image, Color color) {
    var originalColor = image.color;
    for (int n = 0; n < 2; n++) {
      image.color = color;
      yield return new WaitForSeconds(.05f);
      image.color = originalColor;
      yield return new WaitForSeconds(.05f);
    }
  }

  IEnumerator DoDelay(float duration = 1f) {
    delaying = true;
    duration *= battleSpeed;
    yield return new WaitForSeconds(duration);
    delaying = false;
  }

  IEnumerator DoPauseForStagger(float duration = 1f) {
    pausedForStagger = true;
    duration *= battleSpeed;
    yield return new WaitForSeconds(duration);
    pausedForStagger = false;
  }
}
