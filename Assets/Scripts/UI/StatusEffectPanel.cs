using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectPanel : MonoBehaviour {

  public Panel parentPanel;
  public StatusEffectDisplay effectPrefab;
  public List<StatusEffectDisplay> currentDisplays = new List<StatusEffectDisplay>();
  public Queue<StatusEffectDisplay> reusableDisplays = new Queue<StatusEffectDisplay>();

  public void AddEffect(StatusEffect effect) {
    if (currentDisplays.Any(cd => cd.name == effect.name)) { return; }
    var display = DequeueDisplay();
    display.effect = effect;
    display.icon.sprite = effect.sprite;
    if (!string.IsNullOrEmpty(display.effect.actionNameToTrigger)) {
      AddActionToEffect(effect);
    }
    currentDisplays.Add(display);
    if (effect.name == "War Cry" || effect.name == "Charge!") {
      parentPanel.damageDealtPercentMod += .5f;
    } else if (effect.name == "Riposte") {
      var counterattack = currentDisplays.Where(cd => cd.name == "Counterattack").First().effect.actionToTrigger;
      // counterattack.additionalActions.Add(effect);
    }  else if (effect.name == "Bee Sting") {
      parentPanel.damageDealtPercentMod += .3f;
    } else if (effect.name == "En Garde!") {
      Debug.Log("Adding taunting");
      parentPanel.taunting = true;
    }
  }

  public void RemoveEffect(string effectName) {
    for (var i = 0; i < currentDisplays.Count; i++) {
      if (currentDisplays[i].name == effectName) {
        Debug.Log($"Effect name: { currentDisplays[i].name } was removed.");
        EnqueueEntry(currentDisplays[i]);
        if (effectName == "War Cry" || effectName == "Charge!") {
            parentPanel.damageDealtPercentMod -= .5f;
        } else if (effectName == "Bee Sting") {
          parentPanel.damageDealtPercentMod -= .3f;
        } else if (effectName == "En Garde!") {
          parentPanel.taunting = false;
        }
        return;
      }
    }
  }

  public void TriggerEffects(TriggerTypes trigger) {
    ActivateEffects(trigger);
    FadeEffects(trigger);
  }

  void AddActionToEffect(StatusEffect effect) {
    if (effect.actionToTrigger == null) {
      var actionNameToGet = effect.actionNameToTrigger == effect.name ? effect.actionNameToTrigger : effect.name;
      var action = Instantiate(Resources.Load<Action>($"Actions/{ parentPanel.name }/{ actionNameToGet }"));
      if (effect.actionNameToTrigger != effect.name) {
        var parentAction = currentDisplays.Where(cd => cd.name == effect.actionNameToTrigger).FirstOrDefault();
          if (parentAction != null) {
            Debug.Log($"Adding { effect.name } to { parentAction.name }.");
            parentAction.effect.actionToTrigger.additionalActions.Add(action);
          }
        }
      effect.actionToTrigger = action;
    }
  }

  void ActivateEffects(TriggerTypes trigger) {
    for (var i = 0; i < currentDisplays.Count; i++) {
      if (currentDisplays[i].effect.activationTrigger == trigger && currentDisplays[i].effect.actionToTrigger != null) {
        currentDisplays[i].effect.readyToFade = true;
        StartCoroutine(BattleManager.instance.DoHeroAction(currentDisplays[i].effect.actionToTrigger, parentPanel, null, false));
        Debug.Log($"Effect name: { currentDisplays[i].name } activated at { trigger.ToString() }.");
      }
    }
  }

  void FadeEffects(TriggerTypes trigger) {
    for (var i = currentDisplays.Count - 1; i >= 0; --i) {
      if (currentDisplays[i].effect.fadeTrigger == trigger) {
        Debug.Log($"Effect name: { currentDisplays[i].name } faded at { trigger.ToString() }. Total effects: { currentDisplays.Count }");
        EnqueueEntry(currentDisplays[i]);
      }
    }
  }

  void Clear() {
		for (int i = currentDisplays.Count - 1; i >= 0; --i)
			EnqueueEntry(currentDisplays[i]);
	}

	StatusEffectDisplay DequeueDisplay() {
		if (reusableDisplays.Count > 0) {
			StatusEffectDisplay entry = reusableDisplays.Dequeue();
      Debug.Log($"Reusing entry for { entry.name }.");
			entry.gameObject.SetActive(true);
			return entry;
		}
    // Debug.Log($"Creating new entry.");
		var instance = Instantiate(effectPrefab);
		instance.gameObject.transform.SetParent(transform, false);
		return instance;
	}

	void EnqueueEntry(StatusEffectDisplay entry) {
		currentDisplays.Remove(entry);
		reusableDisplays.Enqueue(entry);
		entry.gameObject.SetActive(false);
	}
}
