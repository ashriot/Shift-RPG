using UnityEngine;

public class DestroyOverLifetime : MonoBehaviour {
    
    public float lifetime;

    public void Update() {
      Destroy(gameObject, lifetime);
    }
}
