using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu.Tests
{
    public sealed class RankInfoSliderTests
    {
        private GameObject root;
        private GameObject marker;
        private Slider progress;
        private RankInfoSlider subject;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Rank Slider", typeof(RectTransform));
            progress = root.AddComponent<Slider>();
            marker = new GameObject("Handle Slide Area");
            subject = root.AddComponent<RankInfoSlider>();
            subject.sliderProgress = progress;
            subject.objSlideArea = marker;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(marker);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RewardSegment_UsesPlanetWarHalfScaleAndItsOwnActiveState()
        {
            RankInfoEntry previous = Entry(1003, 200, 0);
            RankInfoEntry reward = Entry(1004, 400, 1);
            RankInfoEntry next = Entry(1005, 650, 1);

            bool active = subject.SetProgress(reward, previous, next, 360);

            Assert.That(active, Is.True);
            Assert.That(progress.value, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(marker.activeSelf, Is.True);
        }

        [Test]
        public void SetValueZero_ClearsThePreviousActiveRowAndHidesItsBadge()
        {
            progress.value = 0.6f;
            marker.SetActive(true);

            subject.SetValue(0f, 520);

            Assert.That(progress.value, Is.EqualTo(0f));
            Assert.That(marker.activeSelf, Is.False);
        }

        [Test]
        public void MissingSerializedMarker_UsesTheSliderHandleArea()
        {
            subject.objSlideArea = null;
            marker.transform.SetParent(root.transform, false);
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(marker.transform, false);
            progress.handleRect = handle.GetComponent<RectTransform>();
            marker.SetActive(true);

            subject.SetValue(0f, 50000);

            Assert.That(marker.activeSelf, Is.False);
            Object.DestroyImmediate(handle);
        }

        [Test]
        public void BoundaryScore_FollowsPlanetWarAndDoesNotForceAnEndBadge()
        {
            RankInfoEntry previous = Entry(1037, 44000, 1);
            RankInfoEntry finalRank = Entry(1038, 50000, 0);

            bool active = subject.SetProgress(finalRank, previous, null, 50000);

            Assert.That(active, Is.False);
            Assert.That(progress.value, Is.EqualTo(0f));
            Assert.That(marker.activeSelf, Is.False);
        }

        private static RankInfoEntry Entry(int id, int score, int type) => new RankInfoEntry
        {
            id = id,
            score = score,
            type = type
        };
    }
}
