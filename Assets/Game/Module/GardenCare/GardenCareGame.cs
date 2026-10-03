using System;
using System.Linq;

namespace Game.GardenCare
{
    public enum GardenCarePhase { Ready, Playing, Result, Closed }

    /// <summary>Pure gameplay state. No rewards or narrative position changes are made here.</summary>
    public sealed class GardenCareGame
    {
        private readonly bool[] _weeds;
        private readonly bool[] _pulled;
        private readonly bool[] _delivered;
        private readonly int[] _insectCells;
        private readonly int[] _companionCells;
        private readonly int[] _weedSides;
        private double _phaseTime;
        public GardenCareConfig Config { get; }
        public GardenCarePhase Phase { get; private set; }
        public string Outcome { get; private set; } = "";
        public string HintKey { get; private set; } = "ui.gardenCare.Intro";
        public double Elapsed { get; private set; }
        public double Remaining => Math.Max(0, Config.timeLimitSeconds - Elapsed);
        public int PlantCount => _pulled.Length;
        public int PulledCount => _pulled.Where((v, i) => v && _weeds[i]).Count();
        public int LostCropCount => _pulled.Where((v, i) => v && !_weeds[i]).Count();
        public int PlantingCell(int index) => IsWeed(index) ? _companionCells[index - Config.cropCount] : index;
        public int WeedSide(int index) => IsWeed(index) ? _weedSides[index - Config.cropCount] : 0;
        public int DeliveredCount => _delivered.Count(v => v);
        public bool IsWeed(int index) => _weeds[index];
        public bool IsPulled(int index) => _pulled[index];
        public bool IsDelivered(int index) => _delivered[index];
        public int InsectCell(int index) => _insectCells[index];

        public GardenCareGame(GardenCareConfig config, int seed)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            // Copy parameters: a caller cannot mutate an active game's validated configuration.
            Config = new GardenCareConfig { tutorial = config.tutorial, cropCount = config.cropCount, weedCount = config.weedCount,
                insectCount = config.insectCount, timeLimitSeconds = config.timeLimitSeconds,
                columns = config.columns, rows = config.rows, resultSeconds=config.resultSeconds,
                tutorialHintDelay=config.tutorialHintDelay, requestSeconds=config.requestSeconds };
            var order = Enumerable.Range(0, config.cropCount).ToArray();
            var random = new Random(seed);
            for (int i = order.Length - 1; i > 0; --i)
            { int j = random.Next(i + 1); int value = order[i]; order[i] = order[j]; order[j] = value; }
            _weeds = new bool[config.cropCount + config.weedCount]; _pulled = new bool[_weeds.Length];
            for (int i = config.cropCount; i < _weeds.Length; i++) _weeds[i] = true;
            _companionCells = order.Take(config.weedCount).ToArray();
            _weedSides = Enumerable.Range(0, config.weedCount).Select(_ => random.Next(2) == 0 ? -1 : 1).ToArray();
            _insectCells = order.OrderBy(_ => random.Next()).Take(config.insectCount).ToArray();
            _delivered = new bool[config.insectCount];
            Start();
        }

        public void Start()
        {
            if (Phase != GardenCarePhase.Ready) return;
            Phase = GardenCarePhase.Playing; _phaseTime = 0; HintKey = "ui.gardenCare.Instruction";
        }

        public void Pull(int index)
        {
            if (Phase != GardenCarePhase.Playing || index < 0 || index >= PlantCount || _pulled[index]) return;
            if (!_weeds[index]) { _pulled[index] = true; HintKey = "ui.gardenCare.CropRemoved"; return; }
            _pulled[index] = true; HintKey = "ui.gardenCare.WeedRemoved"; CheckComplete();
        }

        public void Deliver(int index, bool insideRefuge)
        {
            if (Phase != GardenCarePhase.Playing || index < 0 || index >= _delivered.Length || _delivered[index]) return;
            if (!insideRefuge) { HintKey = "ui.gardenCare.DropInRefuge"; return; }
            _delivered[index] = true; HintKey = "ui.gardenCare.InsectMoved"; CheckComplete();
        }

        private void CheckComplete()
        {
            if (PulledCount == Config.weedCount && DeliveredCount == Config.insectCount) Finish("completed");
        }

        private void Finish(string outcome)
        {
            Outcome = outcome; Phase = GardenCarePhase.Result; _phaseTime = 0;
            HintKey = outcome == "completed" ? "ui.gardenCare.Completed" : "ui.gardenCare.TimeUp";
        }

        public void Continue()
        {
            if (Phase == GardenCarePhase.Result) Phase = GardenCarePhase.Closed;
        }

        public void Tick(double delta, bool paused)
        {
            if (paused || Phase == GardenCarePhase.Closed) return;
            if (double.IsNaN(delta) || double.IsInfinity(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
            // Carry excess time over phase boundaries, so a slow frame cannot extend the bridge indefinitely.
            for (int transitions = 0; delta > 0 && transitions < 3 && Phase != GardenCarePhase.Closed; ++transitions)
            {
                double limit = Phase == GardenCarePhase.Ready ? 0 : Phase == GardenCarePhase.Playing ?
                    (Config.tutorial ? double.PositiveInfinity : Config.timeLimitSeconds) : Config.resultSeconds;
                double step = Math.Min(delta, Math.Max(0, limit - _phaseTime));
                _phaseTime += step; delta -= step;
                if (Phase == GardenCarePhase.Playing) Elapsed = _phaseTime;
                if (_phaseTime < limit) break;
                if (Phase == GardenCarePhase.Ready) Start();
                else if (Phase == GardenCarePhase.Playing) Finish("timeout");
                else Continue();
            }
        }
    }
}
