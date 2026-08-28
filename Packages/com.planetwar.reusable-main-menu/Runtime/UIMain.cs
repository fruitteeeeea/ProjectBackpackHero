using UnityEngine;
using PlanetWar.ReusableMainMenu;
using TMPro;
using UnityEngine.UI;

public class UIMain : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text diamondText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text rankPointsText;
    [SerializeField] private TMP_Text rankProgressText;
    [SerializeField] private TMP_Text rankNameText;
    [SerializeField] private Slider rankProgressSlider;

    public void ApplyProfile(string playerName, int gold, int diamond)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        if (goldText != null) goldText.text = gold.ToString();
        if (diamondText != null) diamondText.text = diamond.ToString();
    }

    public void ApplyRankProgress(int points, int maximumPoints, string rankName)
    {
        ApplyProgress(points, maximumPoints, rankName);
    }

    public void ApplyMainProgress(int points, int maximumPoints, int collectionLevel)
    {
        ApplyProgress(points, maximumPoints, $"Level {collectionLevel}");
    }

    private void ApplyProgress(int points, int maximumPoints, string levelText)
    {
        // Existing prefabs predate this binding. "New Text" is the authored centre
        // label of the rank slider, so discover it once when no serialized reference
        // has been added yet; future prefab edits can simply wire the fields above.
        if (rankProgressText == null)
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.text == "New Text") { rankProgressText = text; break; }
        if (rankProgressSlider == null) rankProgressSlider = GetComponentInChildren<Slider>(true);
        if (rankPointsText != null) rankPointsText.text = points.ToString();
        if (rankProgressText != null) rankProgressText.text = points.ToString("N0");
        if (rankNameText != null) rankNameText.text = levelText;
        if (rankProgressSlider != null) rankProgressSlider.value = Mathf.Clamp01((float)points / Mathf.Max(1, maximumPoints));
    }

    public void OnClickConfirm() => Relay(MainMenuAction.Start);
    public void OnClickSet() => Relay(MainMenuAction.Settings);
    public void OnClickHead() => Relay(MainMenuAction.Profile);
    public void OnClickRank() => Relay(MainMenuAction.Rank);
    private void Relay(MainMenuAction action) => GetComponentInParent<MainMenuActionRelay>()?.Invoke(action);
}
