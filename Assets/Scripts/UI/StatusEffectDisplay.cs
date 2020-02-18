using UnityEngine;
using UnityEngine.UI;

public class StatusEffectDisplay : MonoBehaviour {

    public new string name { get { return effect.name; } }
    public bool active { get { return this.gameObject.activeInHierarchy; } }

    public Image icon;
    public StatusEffect effect;

    // add tooltip
}
