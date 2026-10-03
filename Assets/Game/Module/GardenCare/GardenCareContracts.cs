using System;

namespace Game.GardenCare
{
    [Serializable]
    public sealed class GardenCareConfig
    {
        public bool tutorial;
        public int cropCount;
        public int weedCount;
        public int insectCount;
        public float timeLimitSeconds;
        public int columns;
        public int rows;
        public float resultSeconds = 1.5f, tutorialHintDelay = 3, requestSeconds = 180;

        public void Validate()
        {
            if (columns < 1 || columns > 6 || rows < 1 || rows > 4 || cropCount != columns * rows || cropCount + weedCount > 24 || weedCount < 1 ||
                weedCount > cropCount || insectCount < 1 || insectCount > 4 ||
                insectCount > cropCount || float.IsNaN(timeLimitSeconds) ||
                float.IsInfinity(timeLimitSeconds) || timeLimitSeconds < 10 || timeLimitSeconds > 90)
                throw new ArgumentException("护菜参数无效：菜苗数须等于列×行，总植物数不超过24，虫子1–4只，时限10–90秒。");
            foreach(float v in new[]{resultSeconds,tutorialHintDelay,requestSeconds})
                if(float.IsNaN(v) || float.IsInfinity(v) || v <= 0) throw new ArgumentException("护菜提示和等待时间无效。");
            if(requestSeconds <= timeLimitSeconds + resultSeconds) throw new ArgumentException("护菜请求上限须大于游戏时限与收尾时间之和。");
        }
    }

    public interface IGardenCarePresenter
    {
        void Open(GardenCareGame game, Action start, Action<int> pull,
            Action<int, bool> deliver, Action next, Action<string> failure);
        void Refresh(GardenCareGame game, bool paused);
        void Close();
    }
}
