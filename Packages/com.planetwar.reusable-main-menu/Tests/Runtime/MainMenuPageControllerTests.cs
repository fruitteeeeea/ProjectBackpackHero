using System.Reflection;
using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuPageControllerTests
{
    [Test]
    public void RankInfoOpenHidesBottomBarAndShowsRankInfo()
    {
        var harness = CreateHarness();
        try
        {
            harness.Relay.Invoke(MainMenuAction.Rank);

            Assert.IsTrue(harness.RankInfo.gameObject.activeSelf, "UIRankInfo should be visible.");
            Assert.IsFalse(harness.BottomBar.activeSelf, "Bottom bar should be hidden while UIRankInfo is open.");
            Assert.IsFalse(harness.BottomButton.activeInHierarchy, "Bottom bar buttons should not receive input while UIRankInfo is open.");
            Assert.IsFalse(harness.MainPage.activeSelf, "Main page should be hidden while UIRankInfo is open.");
        }
        finally
        {
            DestroyHarness(harness);
        }
    }

    [Test]
    public void RankInfoBackRestoresMainPageAndBottomBar()
    {
        var harness = CreateHarness();
        try
        {
            harness.Relay.Invoke(MainMenuAction.Rank);
            harness.RankInfo.OnClickClose();

            Assert.IsFalse(harness.RankInfo.gameObject.activeSelf, "UIRankInfo should be hidden after back.");
            Assert.IsTrue(harness.MainPage.activeSelf, "Main page should be restored after back.");
            Assert.IsTrue(harness.BottomBar.activeSelf, "Bottom bar should be restored after back.");
        }
        finally
        {
            DestroyHarness(harness);
        }
    }

    [Test]
    public void RankInfoBackSelectsBattleTargetAndIndex()
    {
        var harness = CreateHarness();
        try
        {
            harness.Relay.Invoke(MainMenuAction.Rank);
            harness.RankInfo.OnClickClose();

            Assert.AreEqual(2, harness.Indicator.ChosenIndex, "The indicator should select the Battle tab index.");
            Assert.AreSame(harness.TabTargets[2], harness.Indicator.ChosenTarget, "The indicator should target Battle instead of the locked placeholder.");
            Assert.AreNotSame(harness.TabTargets[0], harness.Indicator.ChosenTarget, "The indicator must not select the left locked placeholder.");
        }
        finally
        {
            DestroyHarness(harness);
        }
    }

    private sealed class Harness
    {
        public GameObject Root;
        public MainMenuPageController Controller;
        public MainMenuActionRelay Relay;
        public GameObject MainPage;
        public GameObject BottomBar;
        public GameObject BottomButton;
        public RankInfoView RankInfo;
        public Transform[] TabTargets;
        public RecordingBottomChoose Indicator;
    }

    private sealed class RecordingBottomChoose : MainBottmChoose
    {
        public Transform ChosenTarget;
        public int ChosenIndex = -1;

        public override void OnChooseBottom(Transform target, int index)
        {
            ChosenTarget = target;
            ChosenIndex = index;
        }
    }

    private static Harness CreateHarness()
    {
        var root = new GameObject("ControllerRoot", typeof(RectTransform));
        var relay = root.AddComponent<MainMenuActionRelay>();
        var controller = root.AddComponent<MainMenuPageController>();

        var mainPage = new GameObject("MainPage");
        mainPage.transform.SetParent(root.transform, false);
        mainPage.SetActive(true);

        var ranksPage = new GameObject("RanksPage", typeof(RanksView));
        ranksPage.transform.SetParent(root.transform, false);
        ranksPage.SetActive(false);

        var hangarPage = new GameObject("HangarPage", typeof(HangarView));
        hangarPage.transform.SetParent(root.transform, false);
        hangarPage.SetActive(false);

        var rankInfo = new GameObject("UIRankInfo", typeof(RankInfoView));
        rankInfo.transform.SetParent(root.transform, false);
        rankInfo.SetActive(false);

        var bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(root.transform, false);
        bottomBar.SetActive(true);

        var bottomButton = new GameObject("BottomButton", typeof(RectTransform), typeof(Button));
        bottomButton.transform.SetParent(bottomBar.transform, false);
        bottomButton.SetActive(true);

        var indicatorObject = new GameObject("Indicator", typeof(RectTransform));
        indicatorObject.transform.SetParent(root.transform, false);
        var indicator = indicatorObject.AddComponent<RecordingBottomChoose>();

        var tabTargets = new Transform[5];
        for (var i = 0; i < tabTargets.Length; i++)
        {
            var target = new GameObject($"TabTarget_{i}", typeof(RectTransform));
            target.transform.SetParent(root.transform, false);
            ((RectTransform)target.transform).anchoredPosition = new Vector2(i * 100f, 0f);
            tabTargets[i] = target.transform;
        }

        SetField(controller, "mainPage", mainPage);
        SetField(controller, "ranksPage", ranksPage.GetComponent<RanksView>());
        SetField(controller, "hangarPage", hangarPage.GetComponent<HangarView>());
        SetField(controller, "rankInfoPage", rankInfo.GetComponent<RankInfoView>());
        SetField(controller, "bottomBar", bottomBar);
        SetField(controller, "tabIndicator", indicator);
        SetField(controller, "tabTargets", tabTargets);

        return new Harness
        {
            Root = root,
            Controller = controller,
            Relay = relay,
            MainPage = mainPage,
            BottomBar = bottomBar,
            BottomButton = bottomButton,
            RankInfo = rankInfo.GetComponent<RankInfoView>(),
            TabTargets = tabTargets,
            Indicator = indicator
        };
    }

    private static void SetField(object target, string name, object value)
    {
        var field = typeof(MainMenuPageController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing MainMenuPageController field: {name}");
        field.SetValue(target, value);
    }

    private static void DestroyHarness(Harness harness)
    {
        if (harness != null && harness.Root != null) Object.DestroyImmediate(harness.Root);
    }
}
