using UnityEngine;

public class ActionMenu : MonoBehaviour {

  public ActionButton[] actionButtons;

  public Vector3 initialPos;

  private void Start() {
    initialPos = transform.position;
  }
}
