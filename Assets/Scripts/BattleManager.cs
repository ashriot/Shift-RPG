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
  public PopupText popup;

  [Header("Hero/Enemy Refs")]
  public HeroPanel[] heroPanels;
  public EnemyPanel[] enemyPanels;
  public List<Hero> heroes = new List<Hero>();
  public List<Enemy> enemies = new List<Enemy>();
  public List<Enemy> enemyLoadList = new List<Enemy>();

  [Header("Control Vars")]
  public float battleSpeed;
  public bool battleActive;
  public bool battleWaiting;
  public List<Unit> combatants = new List<Unit>();
  public Unit currentCombatant;
  public Enemy playerTarget;
  public Hero enemyTarget;

  private const float INITIATIVE_GROWTH = .05f;

  private void Awake() {
    instance = this;
    DontDestroyOnLoad(gameObject);
  }
  private void Start() {
    actionMenu.gameObject.SetActive(false);
    foreach(var panel in enemyPanels) {
      panel.gameObject.SetActive(false);
    }

    shiftMenu.shiftL.gameObject.SetActive(false);
    shiftMenu.shiftR.gameObject.SetActive(false);
    // TEMPORARY BATTLE START
    var enemyName = "Bat";
    var newEnemy = Instantiate(Resources.Load<Enemy>("Enemies/" + enemyName));
    enemyLoadList.Add(newEnemy);
    InitializeBattle();
  }

  public void InitializeBattle() {
    if (battleActive) return;

    battleActive = true;

    combatants.AddRange(GameManager.instance.heroes);
    combatants.AddRange(enemyLoadList);

    heroes = combatants.Where(c => c.isPlayer).Cast<Hero>().ToList();
    enemies = combatants.Where(c => !c.isPlayer).Cast<Enemy>().ToList();

    for (var i = 0; i < heroPanels.Length; i++) {
      if (i >= heroes.Count) { 
        heroPanels[i].gameObject.SetActive(false);
        heroPanels[i].GetComponentInParent<RectTransform>().gameObject.SetActive(false);
        continue;
      }

      var panel = heroPanels[i];
      panel.gameObject.SetActive(true);
      panel.GetComponentInParent<RectTransform>().gameObject.SetActive(true);
      panel.hero = heroes[i];
      panel.hero.hpCurrent = panel.hero.hp;
      panel.hero.armorCurrent = panel.hero.armor;
      panel.hero.mpCurrent = panel.hero.mp;
      panel.currentJobName.text = panel.hero.currentJob.name;
      panel.hpFillImage.fillAmount = panel.hero.hpPercent;
      panel.currentHp.text = panel.hero.hp.ToString();
      panel.currentArmor.text = panel.hero.armorCurrent.ToString();
      panel.currentMp.text = panel.hero.mpCurrent.ToString();
    }

    for(var i = 0; i < enemyPanels.Length; i++) {
      if (i > enemies.Count) { 
        enemyPanels[i].gameObject.SetActive(false);
        continue;
      }
      var panel = enemyPanels[i];
      panel.gameObject.SetActive(true);
      panel.enemy = enemies[i];
      panel.enemy.hpCurrent = panel.enemy.hp;
      panel.enemy.armorCurrent = panel.enemy.armor;
      panel.enemy.mpCurrent = panel.enemy.mp;
      panel.hpFillImage.fillAmount = panel.enemy.hpPercent;
      panel.currentArmor.text = panel.enemy.armorCurrent.ToString();
      panel.currentMp.text = panel.enemy.mpCurrent.ToString();
    }
    SetInitialTicks();
    NextTurn();
  }

  void FixedUpdate() {
    if (!battleActive || !battleWaiting) { return; }
    // These values must be TRUE to continue
    battleWaiting = false;

    // check status effects

    if (currentCombatant.isPlayer) {
      Debug.Log("Player turn");
      // PLAYER TURN
      if (combatants.All(x => x.isDead && !x.isPlayer)) return;
      var hero = currentCombatant as Hero;
      
      for(var i = 0; i < actionMenu.actionButtons.Length; i++) {
        if (hero.currentJob.actions[i] != null) {
          actionMenu.actionButtons[i].gameObject.SetActive(true);
          actionMenu.actionNames[i].text = hero.currentJob.actions[i].name;
          actionMenu.actionCosts[i].text = (hero.currentJob.actions[i].cost / 10).ToString();
        } else {
          actionMenu.actionButtons[i].gameObject.SetActive(false);
        }
      }

      if (hero.jobs.Length != 1) {
        shiftMenu.shiftL.gameObject.SetActive(true);
        shiftMenu.shiftR.gameObject.SetActive(true);
        shiftMenu.shiftL.GetComponentInChildren<Text>().text = "Shift: " + hero.jobs[0].name;
        if (hero.jobs.Length == 2) {
          shiftMenu.shiftR.GetComponentInChildren<Text>().text = "Shift: " +hero.jobs[0].name;
        } else {
          shiftMenu.shiftR.GetComponentInChildren<Text>().text = "Shift: " +hero.jobs[1].name;
        }
      } else         { 
        shiftMenu.shiftL.gameObject.SetActive(false);
        shiftMenu.shiftR.gameObject.SetActive(false);
      }

      actionMenu.gameObject.SetActive(true);
    } else {
      Debug.Log("Enemy turn");
      // ENEMY TURN
      var taunters = heroes.Where(h => h.isTaunting).ToList();
      if (taunters.Count > 0) {
        enemyTarget = taunters.First();
      } else {
        // random target
        var random = Random.Range(0, heroes.Count);
        enemyTarget = heroes[random];
      }
      EnemyAction();
    }
  }

  private void SetInitialTicks() {
      foreach (var c in combatants) {
          CalculateSpeedTicks(c, 1f);
      }
  }

  private void NextTurn() {
    actionMenu.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    // check for battle end

    UpdatePlayerTarget();

    CountdownTicks();

  }

  private void UpdateUi() {
    for (var i = 0; i < heroes.Count; i++) {
      var panel = heroPanels.Where(hp => hp.hero == heroes[i]).Single();
      panel.hero.hpCurrent = panel.hero.hp;
      panel.hero.armorCurrent = panel.hero.armor;
      panel.hero.mpCurrent = panel.hero.mp;
      panel.currentJobName.text = panel.hero.currentJob.name;
      panel.hpFillImage.fillAmount = panel.hero.hpPercent;
      panel.currentHp.text = panel.hero.hp.ToString();
      panel.currentArmor.text = panel.hero.armorCurrent.ToString();
      panel.currentMp.text = panel.hero.mpCurrent.ToString();
    }

    for(var i = 0; i < enemies.Count; i++) {
      var panel = enemyPanels.Where(ep => ep.enemy == enemies[i]).Single();
      panel.enemy.hpCurrent = panel.enemy.hp;
      panel.enemy.armorCurrent = panel.enemy.armor;
      panel.enemy.mpCurrent = panel.enemy.mp;
      panel.hpFillImage.fillAmount = panel.enemy.hpPercent;
      panel.currentArmor.text = panel.enemy.armorCurrent.ToString();
      panel.currentMp.text = panel.enemy.mpCurrent.ToString();
    }
  }

  private void UpdatePlayerTarget() {
    if (playerTarget == null) {
      playerTarget = combatants.Where(c => !c.isPlayer).Cast<Enemy>().First();
    }

    foreach (var panel in enemyPanels) {
      if (panel.enemy = playerTarget) {
        panel.targetCursor.gameObject.SetActive(true);
      } else {
        panel.targetCursor.gameObject.SetActive(false);
      }
    }
  }

  private void CalculateSpeedTicks(Unit combatant, float delay) {
    var inverse = Mathf.Pow((1 + INITIATIVE_GROWTH), combatant.agility);
    var result = 1000 / inverse;
    result *= delay;
    combatant.ticks = (int)result;
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
        if (!combatant.isDead && combatant.ticks == 0) {
          currentCombatant = combatant;
          battleWaiting = true;
          return;
        } else {
          // Debug.Log(combatant.name + ": " + combatant.ticks);
          combatant.ticks--;
        }
      }
    }
  }

  public void ClickActionButton(int buttonId) {
    Action actionToExecute = null;
    var actionName = actionMenu.actionNames[buttonId].text;
    var hero = currentCombatant as Hero;
    foreach(var action in hero.currentJob.actions) {
      if (action.name == actionName) {
        actionToExecute = action;
        break;
      }
    }
    if (actionToExecute != null) {
      HeroAction(actionToExecute);
    } else {
      Debug.Log("action is null!");
    }
  }

  public void HeroAction(Action action) {
    actionMenu.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    Debug.Log("Executing: " + action.name);
    StartCoroutine(HeroActionCoroutine(action));
  }

  public IEnumerator HeroActionCoroutine(Action action) {
    var panel = heroPanels.Where(hp => hp.hero == currentCombatant).Single();
    var position = panel.gameObject.transform.position;
    Debug.Log("Hero pos: " + position);
    position.y += panel.GetComponent<RectTransform>().rect.height;
    Instantiate(popup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(action.name, battleSpeed * .8f, false);
    yield return new WaitForSeconds(battleSpeed);
    CalculateSpeedTicks(currentCombatant, 1f);
    Debug.Log("Hero turn done.");
    NextTurn();
  }

  public void EnemyAction() {
    var panel = enemyPanels.Where(ep => ep.enemy == currentCombatant).Single();
    StartCoroutine(FlashImage(panel.enemyImage, Color.clear));
    var action = ScriptableObject.CreateInstance<Action>();
    action.name = "Sonic Blast";
    action.potency = 20;
    action.damageType = DamageTypes.Ether;
    Debug.Log("Executing: " + action.name);
    StartCoroutine(EnemyActionCoroutine(action));
  }

  private IEnumerator EnemyActionCoroutine(Action action) {
    yield return new WaitForSeconds(battleSpeed / 2f);

    var panel = enemyPanels.Where(ep => ep.enemy == currentCombatant).Single();
    var position = panel.gameObject.transform.position;
    Debug.Log("Enemy pos: " + position);
    position.y += panel.GetComponent<RectTransform>().rect.height * 2;
    position.x *= 1.225f;
    position.y /= 1.8f;
    Instantiate(popup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(action.name, battleSpeed * 0.8f, false);
    yield return new WaitForSeconds(battleSpeed / 2f);

    EnemyDealDamage(action);
    yield return new WaitForSeconds(battleSpeed);

    CalculateSpeedTicks(currentCombatant, 1f);
    Debug.Log("Enemy turn done.");
    NextTurn();
  }

  private void HeroDealDamage(Action action) {
    var attacker = currentCombatant as Hero;
  }

  private void EnemyDealDamage(Action action) {
    Debug.Log("Enemy dealing damage");
    var attacker = currentCombatant as Enemy;
    var defender = enemyTarget;
    
    var damage = action.potency;
    var defense = 0f;

    if (action.damageType == DamageTypes.Martial) {
      defense = defender.currentJob.martialDefense;
    } else if (action.damageType == DamageTypes.Ether) {
      defense = defender.currentJob.etherDefense;
    }

    Debug.Log("Damage before def: " + damage + " def: " + defense);

    damage = (int)((float)damage * (1f - defense));

    Debug.Log("Damage after def: " + damage);

    defender.hpCurrent -= damage;
    Debug.Log(defender.name + "'s HP: " + defender.hpCurrent);

    var panel = heroPanels.Where(hp => hp.hero == defender).Single();
    var position = panel.gameObject.transform.position;
    position.y += panel.GetComponent<RectTransform>().rect.height * 2;
    Instantiate(popup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage.ToString("N0"), battleSpeed * 0.8f);

    UpdateUi();
  }

  public IEnumerator<WaitForSeconds> FlashImage(Image image, Color color) {
    var originalColor = image.color;
    for (int n = 0; n < 2; n++) {
      image.color = color;
      yield return new WaitForSeconds(.05f);
      image.color = originalColor;
      yield return new WaitForSeconds(.05f);
    }
  }

}
