using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game.TalismanPractice
{
    public sealed class TalismanPracticeGame
    {
        public TalismanPracticeConfig Config { get; }
        public PracticeStage Stage { get; private set; }
        public PracticePhase Phase { get; private set; } = PracticePhase.Ready;
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public float TransitionRemaining { get; private set; }
        public float Patience { get; private set; }
        public int Round { get; private set; }
        public int Customer { get; private set; }
        public int Pattern => Stage == PracticeStage.Differences ? Round : _orders[Math.Min(Customer, _orders.Length-1)];
        public int Revision { get; private set; }
        public float FeatherY { get; private set; }
        public float FeatherVelocity { get; private set; }
        public float FeatherX => .5f + Config.driftAmplitude * Mathf.Sin(Elapsed * Config.driftFrequency) + Config.secondaryDriftAmplitude * Mathf.Sin(Elapsed * Config.secondaryDriftFrequency);
        public float FeatherTilt => Config.tiltDegrees * Mathf.Sin(Elapsed * Config.driftFrequency);
        public float FrameX => .5f + Config.frameMoveAmplitudeX * Mathf.Sin(Elapsed * 2 * Mathf.PI / Config.frameMovePeriod);
        public float FrameOffsetY => Config.frameMoveAmplitudeY * Mathf.Sin(Elapsed * 2 * Mathf.PI / Config.frameMovePeriod);
        public float FrameBottom => Config.frameBottom + FrameOffsetY;
        public float FrameTop => Config.frameTop + FrameOffsetY;
        public bool Blowing { get; private set; }
        public float BlowStrength { get; private set; }
        public float HoldTime { get; private set; }
        public bool InFrame => FeatherY - Config.featherHalfHeight >= FrameBottom && FeatherY + Config.featherHalfHeight <= FrameTop
            && FeatherX - Config.featherHalfWidth >= FrameX - Config.frameWidth / 2 && FeatherX + Config.featherHalfWidth <= FrameX + Config.frameWidth / 2;
        public string Hint { get; private set; } = "对照左右符纸，找出错笔。点开始进入练习。";
        public TalismanPracticeResult Result { get; } = new TalismanPracticeResult();
        private readonly bool[] _found = new bool[2];
        private readonly int[] _orders;
        private readonly int[] _parts = { -1, -1, -1 };
        public int SelectedPart(int layer) => layer >= 0 && layer < 3 ? _parts[layer] : -1;
        public int[] SelectedParts => (int[])_parts.Clone();
        public TalismanPracticeGame(TalismanPracticeConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate(); Config = config.Copy(); _orders = new int[Config.customerCount];
            Stage = Config.stage;
            FeatherY = Config.startY;
            Result.differences = Result.orders = Result.feather = "not_selected";
            Hint = Stage == PracticeStage.Differences ? "对照左右符纸，找出错笔。" : Stage == PracticeStage.Orders
                ? "按顾客的符样搭配底纹、中层、细节。" : "按住鼠标或空格缓缓吹气，松开让羽毛飘落。";
            for(int i=0;i<_orders.Length;i++) _orders[i] = i % 3;
            Start();
        }
        public bool Found(int id) => id >= 0 && id < 2 && _found[id];
        public void Start()
        {
            if (Phase != PracticePhase.Ready) return;
            Phase = PracticePhase.Playing;
            Remaining = Stage == PracticeStage.Differences ? Config.differenceSeconds : Stage == PracticeStage.Orders ? Config.orderSeconds : Config.featherSeconds;
            Patience = Config.patienceSeconds;
            Hint = Stage == PracticeStage.Differences ? "点击任意一侧的两处差异。" : Stage == PracticeStage.Orders
                ? "分别选择底纹、中层、细节，组合好后交给顾客。" : "按住吹气按钮或空格，气流会逐渐变强；松开便停止吹气。";
            Revision++;
        }
        public void FindDifference(int id)
        {
            if (Phase != PracticePhase.Playing || Stage != PracticeStage.Differences) return;
            if (id < 0 || id > 1) { Hint = "这里相同，再看看笔划。"; return; }
            if (_found[id]) return;
            _found[id] = true; Result.differencesFound++; Hint = "找到一处！";
            if (_found[0] && _found[1])
            {
                if (Round == 2) { Result.differences = "completed"; Review("三种符样辨认完成。回顾正确符样后返回剧情。"); }
                else { Round++; _found[0] = _found[1] = false; Hint = "下一张：" + TalismanPatterns.Name(Round); }
            }
            Revision++;
        }
        public void SelectPart(int layer, int variant)
        {
            if (Phase != PracticePhase.Playing || Stage != PracticeStage.Orders || layer < 0 || layer > 2 || variant < 0 || variant > 2) return;
            _parts[layer] = variant; Revision++;
            Hint = "已选择" + TalismanPatterns.LayerName(layer) + "：" + TalismanPatterns.PartName(layer, variant) + "。";
        }
        public void Clear()
        {
            if (Phase != PracticePhase.Playing || Stage != PracticeStage.Orders) return;
            ResetParts(); Hint = "已清空，重新搭配三部分。";
        }
        private void ResetParts() { for (int i=0;i<3;i++) _parts[i]=-1; Revision++; }
        public bool Submit()
        {
            if (Phase != PracticePhase.Playing || Stage != PracticeStage.Orders) return false;
            for (int i=0;i<3;i++)
            {
                if (_parts[i]<0) { Hint = "还没有选择" + TalismanPatterns.LayerName(i) + "。"; return false; }
                if (_parts[i]!=TalismanPatterns.PartFor(Pattern,i))
                { Hint = TalismanPatterns.LayerName(i) + "与顾客需求不符，可以直接换一个。"; return false; }
            }
            Result.ordersCompleted++; NextCustomer("顾客满意地收下了符。"); return true;
        }
        private void NextCustomer(string message)
        {
            Customer++; ResetParts();
            if (Customer >= Config.customerCount)
            {
                Result.orders = Result.ordersCompleted == Config.customerCount ? "completed" : "partial";
                Review("接待结束，返回剧情。");
            }
            else { Patience = Config.patienceSeconds; Hint = message + " 下一位要：" + TalismanPatterns.Name(Pattern); }
        }
        public void SetBlowing(bool held)
        {
            Blowing = held && Phase == PracticePhase.Playing && Stage == PracticeStage.Feather;
            if (!Blowing) BlowStrength = 0;
        }
        public void Next()
        {
            if (Phase != PracticePhase.Review) return;
            Phase = PracticePhase.Finished;
            Revision++;
        }
        private void Review(string message) { Phase=PracticePhase.Review; SetBlowing(false); TransitionRemaining=Config.reviewSeconds; Hint=message; Revision++; }
        public void Tick(float delta, bool paused)
        {
            if(paused || Phase == PracticePhase.Finished || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            // Fixed small steps keep timer boundaries and feather simulation stable after a frame hitch.
            float left = Mathf.Min(delta, Config.requestSeconds);
            while(left > .000001f && Phase != PracticePhase.Finished)
            {
                float dt = Mathf.Min(left, 1f/60f); left -= dt;
                if(Phase == PracticePhase.Ready || Phase == PracticePhase.Review)
                {
                    TransitionRemaining = Mathf.Max(0, TransitionRemaining-dt);
                    if(TransitionRemaining <= 0) { if(Phase == PracticePhase.Ready) Start(); else Next(); }
                    continue;
                }
                Elapsed += dt;
                if(!Config.tutorial) { dt = Mathf.Min(dt, Remaining); Remaining = Mathf.Max(0, Remaining-dt); }
                if(Stage == PracticeStage.Orders)
                {
                    if(!Config.tutorial) Patience = Mathf.Max(0, Patience-dt);
                    if(Patience <= 0) NextCustomer("这位顾客等不及，先离开了。");
                }
                else if(Stage == PracticeStage.Feather)
                {
                    BlowStrength = Blowing ? Mathf.Min(1, BlowStrength + dt / Config.blowRampSeconds) : 0;
                    float acceleration = BlowStrength * Config.blowAcceleration - Config.gravity - Config.airDrag * FeatherVelocity;
                    FeatherVelocity = Mathf.Clamp(FeatherVelocity + acceleration * dt, -Config.maxFallSpeed, Config.maxRiseSpeed);
                    FeatherY += FeatherVelocity*dt;
                    if(FeatherY < Config.featherHalfHeight || FeatherY > 1-Config.featherHalfHeight)
                    { FeatherY=Mathf.Clamp(FeatherY,Config.featherHalfHeight,1-Config.featherHalfHeight); FeatherVelocity=0; }
                    if(InFrame) HoldTime=Mathf.Min(Config.holdSeconds,HoldTime+dt);
                    Result.holdMilliseconds=Mathf.RoundToInt(HoldTime*1000);
                    if(HoldTime >= Config.holdSeconds) { Result.feather="completed"; Review("呼吸平稳，练习完成。"); }
                }
                if(!Config.tutorial && Phase == PracticePhase.Playing && Remaining <= 0)
                {
                    if(Stage == PracticeStage.Differences) Result.differences="timeout";
                    else if(Stage == PracticeStage.Orders) Result.orders="timeout";
                    else Result.feather="timeout";
                    Review(Stage == PracticeStage.Differences ? "时间到了。回顾下方正确符样后返回剧情。" : "这轮练习到这里，返回剧情。");
                }
            }
        }
    }
}
