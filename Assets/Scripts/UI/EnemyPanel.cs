public class EnemyPanel : Panel {

  public override void Setup() {
    base.Setup();
    var enemy = unit as Enemy;
    image.sprite = enemy.sprite;
    infoTooltip.SetupTooltip(enemy.name, "Enemy".ToUpper(), "NA", enemy.tooltipDescription);
  }
}
