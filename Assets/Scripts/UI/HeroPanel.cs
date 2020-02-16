using UnityEngine.UI;

public class HeroPanel : Panel {

  public Text unitNameText;
  public Text currentJobName;
  public Image jobColor, jobIcon;

  public override void Setup() {
    base.Setup();
    
    var hero = unit as Hero;
    jobColor.color = hero.currentJob.jobColor;
    jobIcon.sprite = hero.currentJob.jobIcon;
    unit.hpCurrent = unit.hpMax;
    unit.armorCurrent = unit.armorMax;
    unit.mpCurrent = unit.mp;
    unitNameText.text = name;
    currentJobName.text = hero.currentJob.name.ToUpper();
    hpFillImage.fillAmount = unit.hpPercent;
    currentHp.text = unit.hpMax.ToString("N0");
  }
}
