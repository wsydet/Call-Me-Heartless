using System;
using UnityEngine;
namespace Game.TalismanPractice
{
    public enum PracticeStage { Differences, Orders, Feather }
    public enum PracticePhase { Ready, Playing, Review, Finished }
    [Serializable]
    public sealed class TalismanPracticeConfig
    {
        public PracticeStage stage = PracticeStage.Differences;
        public bool tutorial;
        public int difficultyLevel = 1;
        public string difficultyName = "入门呼吸";
        public float differenceSeconds = 90, orderSeconds = 150, patienceSeconds = 25;
        public int customerCount = 5;
        public float featherSeconds = 60, holdSeconds = 10;
        public float reviewSeconds = 1.5f, requestSeconds = 600, tutorialHintDelay = 3;
        public float gravity = .2f, blowAcceleration = .42f, blowRampSeconds = .85f;
        public float airDrag = 2f, maxRiseSpeed = .12f, maxFallSpeed = .1f;
        public float frameBottom = .27f, frameTop = .79f, featherHalfHeight = .04f;
        public float frameWidth = .8f, frameMoveAmplitudeX, frameMoveAmplitudeY, frameMovePeriod = 12;
        public float featherHalfWidth = .022f, startY = .2f;
        public float driftAmplitude = .075f, driftFrequency = 1.7f, secondaryDriftAmplitude = .02f, secondaryDriftFrequency = .73f, tiltDegrees = 12;
        public void Validate()
        {
            if (!Enum.IsDefined(typeof(PracticeStage), stage)) throw new ArgumentException("请选择有效的学习项目。");
            foreach (float x in new[] { differenceSeconds, orderSeconds, patienceSeconds,
                holdSeconds, reviewSeconds, requestSeconds, gravity, blowAcceleration,
                blowRampSeconds, airDrag, maxRiseSpeed, maxFallSpeed, frameMovePeriod, driftFrequency, secondaryDriftFrequency })
                if (float.IsNaN(x) || float.IsInfinity(x) || x <= 0) throw new ArgumentException("练习时限和物理参数必须是正有限数。");
            foreach (float x in new[] { featherSeconds, featherHalfHeight, featherHalfWidth, frameBottom, frameTop, frameWidth,
                frameMoveAmplitudeX, frameMoveAmplitudeY, startY, driftAmplitude, secondaryDriftAmplitude, tiltDegrees, tutorialHintDelay })
                if(float.IsNaN(x) || float.IsInfinity(x) || x < 0) throw new ArgumentException("羽毛范围、移动和提示参数必须是非负有限数。");
            if (customerCount < 3 || customerCount > 20 || difficultyLevel < 1 ||
                featherHalfHeight <= 0 || float.IsNaN(featherHalfHeight) || float.IsNaN(frameBottom) || float.IsNaN(frameTop) ||
                frameBottom < 0 || frameTop > 1 || frameTop - frameBottom <= featherHalfHeight * 2 ||
                (!tutorial && (featherSeconds <= 0 || holdSeconds > featherSeconds)) ||
                featherHalfWidth <= 0 || frameWidth > 1 || frameWidth <= 2 * featherHalfWidth ||
                frameBottom - frameMoveAmplitudeY < 0 || frameTop + frameMoveAmplitudeY > 1 ||
                frameWidth / 2 + frameMoveAmplitudeX > .5f ||
                startY < featherHalfHeight || startY > 1 - featherHalfHeight ||
                driftAmplitude + secondaryDriftAmplitude + featherHalfWidth > .5f || tiltDegrees > 45 ||
                blowAcceleration <= gravity)
                throw new ArgumentException("练习配置超出有效范围。");
            float longest = stage==PracticeStage.Differences ? differenceSeconds : stage==PracticeStage.Orders ? orderSeconds : featherSeconds;
            if(!tutorial && requestSeconds <= longest + reviewSeconds) throw new ArgumentException("请求等待上限须大于本关时限与收尾时间之和。");
        }
        public TalismanPracticeConfig Copy() => JsonUtility.FromJson<TalismanPracticeConfig>(JsonUtility.ToJson(this));
    }
    [Serializable]
    public sealed class TalismanPracticeResult
    {
        public string differences, orders, feather;
        public int differencesFound, ordersCompleted, holdMilliseconds;
    }
    public interface ITalismanPracticeService
    {
        void Invoke(string requestKey, string id, TalismanPracticeConfig config, Action<string> complete, Action<string> fail);
        void Abort(string id);
    }
    public interface ITalismanPracticePresenter
    {
        void Open(TalismanPracticeGame game, Action start, Action next, Action<int> difference,
            Action clear, Action submit, Action<bool> blow, Action<int, int> select, Action<string> fail);
        void Refresh(TalismanPracticeGame game, bool paused);
        void Close();
    }
}
