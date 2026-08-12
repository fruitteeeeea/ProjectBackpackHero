using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Compatibility controller for the unmodified migrated original UIBattle prefab.</summary>
public sealed class UIBattle : MonoBehaviour
{
    public GameObject objSearch;
    public GameObject objBattle;
    public ItemBattle itemMy;
    public ItemBattle itemOpponent;
    public float searchMinTime = 1.5f;
    public float searchMaxTime = 3.5f;

    private Tween switchTween;
    public bool IsRunning { get; private set; }

    public void BeginMatch(MatchParticipant player, MatchParticipant opponent)
    {
        if (IsRunning)
            return;

        IsRunning = true;
        gameObject.SetActive(true);
        itemMy.SetData(player, true);
        itemOpponent.SetData(opponent, false);
        objSearch.SetActive(true);
        objBattle.SetActive(false);

        switchTween?.Kill(false);
        float min = Mathf.Min(searchMinTime, searchMaxTime);
        float max = Mathf.Max(searchMinTime, searchMaxTime);
        switchTween = DOTween.Sequence()
            .AppendInterval(Random.Range(min, max))
            .AppendCallback(() =>
            {
                objSearch.SetActive(false);
                objBattle.SetActive(true);
                itemMy.RunAnimation();
                itemOpponent.RunAnimation();
            })
            .AppendInterval(1f)
            .AppendCallback(LoadBattleScene);
    }

    private void LoadBattleScene()
    {
        StartCoroutine(LoadBattleSceneAsync());
    }

    private IEnumerator LoadBattleSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError("Could not load SampleScene. Add it to Build Settings.", this);
            IsRunning = false;
            gameObject.SetActive(false);
            yield break;
        }

        while (!operation.isDone)
            yield return null;
    }

    private void OnDisable()
    {
        switchTween?.Kill(false);
        switchTween = null;
        IsRunning = false;
    }
}
