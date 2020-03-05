using UnityEngine;
using UnityEngine.UI;

public class ShiftMenu : MonoBehaviour {

  public Image colorL, colorR, traitColor, shiftColor;
  public Text nameL, nameR, traitName, shiftName, selectTarget;
  public Image jobIconL, jobIconR, traitIcon, shiftIcon;
  public int jobIdL, jobIdR;
  public TooltipButton traitTooltipButton, shiftActionTooltipButton, shiftLTooltipButton, shiftRTooltipButton;

  public Vector2 initialSize;

  public void Click(int buttonId) {
    var jobId = 0;
    if (buttonId == 0) {
      jobId = jobIdL;
    } else {
      jobId = jobIdR;
    }
    BattleManager.instance.ClickShiftButton(jobId);
  }

  public void DisplayShiftMenu(Hero hero) {
    if (hero.currentJob.trait != null) {
      traitTooltipButton.SetupTooltip(hero.currentJob.trait.name, "Passive Trait".ToUpper(), "NA", hero.currentJob.trait.description);
      traitColor.color = hero.currentJob.jobColor;
      traitName.text = hero.currentJob.trait.name;
      traitIcon.sprite = hero.currentJob.trait.sprite;
    }
    if (hero.currentJob.shiftAction != null) {
      shiftActionTooltipButton.SetupTooltip(hero.currentJob.shiftAction.name, "Shift Action".ToUpper(), "NA", hero.currentJob.shiftAction.description);
      shiftColor.color = hero.currentJob.jobColor;
      shiftName.text = hero.currentJob.shiftAction.name;
      shiftIcon.sprite = hero.currentJob.shiftAction.sprite;
    }

    if (hero.jobs.Length > 1) {
      var jobIndex = System.Array.IndexOf(hero.jobs, hero.currentJob);
      var jobLIndex = (jobIndex - 1) < 0 ? hero.jobs.Length - 1 : jobIndex - 1;
      var jobRIndex = (jobIndex + 1) == hero.jobs.Length ? 0 : jobIndex + 1;
      var jobL = hero.jobs[jobLIndex];
      var jobR = hero.jobs[jobRIndex];
      gameObject.SetActive(true);
      nameL.text = jobL.name;
      jobIconL.sprite = jobL.jobIcon;
      colorL.color = jobL.jobColor;
      jobIdL = jobLIndex;
      shiftLTooltipButton.SetupTooltip(jobL.name, "Job".ToUpper(), "NA", jobL.description, jobL);
      nameR.text = jobR.name;
      jobIconR.sprite = jobR.jobIcon;
      colorR.color = jobR.jobColor;
      jobIdR = jobRIndex;
      shiftRTooltipButton.SetupTooltip(jobR.name, "Job".ToUpper(), "NA", jobR.description, jobR);
    } else {
      gameObject.SetActive(false);
    }
  }

  void Start() {
    // initialSize = GetComponent<RectTransform>().sizeDelta;
  }

}
