using UnityEngine;
using UnityEngine.UI;

public class StatusEffectDisplay : MonoBehaviour {
    public new string name { get { return effect.effectName; } }
    public Image icon;
    public StatusEffect effect;
}
