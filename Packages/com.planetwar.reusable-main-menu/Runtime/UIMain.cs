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
    private string originalRankNameText;

    public void ApplyProfile(string playerName, int gold, int diamond)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        if (goldText != null) goldText.text = gold.ToString();
        if (diamondText != null) diamondText.text = diamond.ToString();
    }

    public void ApplyRankProgress(int points, int maximumPoints, string rankName)
    {
        // The original main-menu presentation did not bind this label to rank data;
        // preserve its authored text ("Level 1") when the milestone UI is enabled.
        ApplyProgress(points, maximumPoints, GetOriginalRankNameText(), true);
    }

    public void ApplyMainProgress(int points, int maximumPoints, int collectionLevel)
    {
        GetOriginalRankNameText();
        ApplyProgress(points, maximumPoints, $"Level {collectionLevel}", false);
    }

    private string GetOriginalRankNameText()
    {
        if (originalRankNameText == null && rankNameText != null)
            originalRankNameText = rankNameText.text;
        return originalRankNameText ?? string.Empty;
    }

    private void ApplyProgress(int points, int maximumPoints, string levelText, bool showMaximumPoints)
    {
        // Existing prefabs predate this binding. "New Text" is the authored centre
        // label of the rank slider, so discover it once when no serialized reference
        // has been added yet; future prefab edits can simply wire the fields above.
        if (rankProgressText == null)
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.text == "New Text") { rankProgressText = text; break; }
        if (rankProgressSlider == null) rankProgressSlider = GetComponentInChildren<Slider>(true);
        if (rankPointsText != null) rankPointsText.text = points.ToString();
        if (rankProgressText != null)
            rankProgressText.text = showMaximumPoints
                ? $"{points:N0}/{maximumPoints:N0}"
                : points.ToString("N0");
        if (rankNameText != null) rankNameText.text = levelText;
        if (rankProgressSlider != null) rankProgressSlider.value = Mathf.Clamp01((float)points / Mathf.Max(1, maximumPoints));
    }

    public void OnClickConfirm() => Relay(MainMenuAction.Start);
    public void OnClickSet() => Relay(MainMenuAction.Settings);
    public void OnClickHead() => Relay(MainMenuAction.Profile);
    public void OnClickRank() => Relay(MainMenuAction.Rank);
    private void Relay(MainMenuAction action) => GetComponentInParent<MainMenuActionRelay>()?.Invoke(action);
}
