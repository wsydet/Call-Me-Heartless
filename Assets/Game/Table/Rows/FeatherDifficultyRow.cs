using Ember.Table;

namespace Game.Table
{
    [EmberTable("feather_difficulty")]
    public sealed class FeatherDifficultyRow
    {
        [EmberTableKey, EmberTableColumn("id")] public string Id { get; }
        [EmberTableColumn("level")] public int Level { get; }
        [EmberTableColumn("label")] public string Label { get; }
        [EmberTableColumn("timeLimitSeconds")] public float TimeLimitSeconds { get; }
        [EmberTableColumn("holdSeconds")] public float HoldSeconds { get; }
        [EmberTableColumn("gravity")] public float Gravity { get; }
        [EmberTableColumn("blowAcceleration")] public float BlowAcceleration { get; }
        [EmberTableColumn("blowRampSeconds")] public float BlowRampSeconds { get; }
        [EmberTableColumn("airDrag")] public float AirDrag { get; }
        [EmberTableColumn("maxRiseSpeed")] public float MaxRiseSpeed { get; }
        [EmberTableColumn("maxFallSpeed")] public float MaxFallSpeed { get; }
        [EmberTableColumn("frameBottom")] public float FrameBottom { get; }
        [EmberTableColumn("frameTop")] public float FrameTop { get; }
        [EmberTableColumn("frameWidth")] public float FrameWidth { get; }
        [EmberTableColumn("frameMoveAmplitudeX")] public float FrameMoveAmplitudeX { get; }
        [EmberTableColumn("frameMoveAmplitudeY")] public float FrameMoveAmplitudeY { get; }
        [EmberTableColumn("frameMovePeriod")] public float FrameMovePeriod { get; }
        [EmberTableColumn("featherHalfWidth")] public float FeatherHalfWidth { get; }
        [EmberTableColumn("featherHalfHeight")] public float FeatherHalfHeight { get; }
        [EmberTableColumn("startY")] public float StartY { get; }
        [EmberTableColumn("driftAmplitude")] public float DriftAmplitude { get; }
        [EmberTableColumn("driftFrequency")] public float DriftFrequency { get; }
        [EmberTableColumn("secondaryDriftAmplitude")] public float SecondaryDriftAmplitude { get; }
        [EmberTableColumn("secondaryDriftFrequency")] public float SecondaryDriftFrequency { get; }
        [EmberTableColumn("tiltDegrees")] public float TiltDegrees { get; }
        [EmberTableColumn("tutorialHintDelay")] public float TutorialHintDelay { get; }
        [EmberTableColumn("resultSeconds")] public float ResultSeconds { get; }

        [EmberTableConstructor]
        public FeatherDifficultyRow(string id, int level, string label, float timeLimitSeconds, float holdSeconds, float gravity, float blowAcceleration, float blowRampSeconds, float airDrag, float maxRiseSpeed, float maxFallSpeed, float frameBottom, float frameTop, float frameWidth, float frameMoveAmplitudeX, float frameMoveAmplitudeY, float frameMovePeriod, float featherHalfWidth, float featherHalfHeight, float startY, float driftAmplitude, float driftFrequency, float secondaryDriftAmplitude, float secondaryDriftFrequency, float tiltDegrees, float tutorialHintDelay, float resultSeconds)
        {
            Id = id;
            Level = level;
            Label = label;
            TimeLimitSeconds = timeLimitSeconds;
            HoldSeconds = holdSeconds;
            Gravity = gravity;
            BlowAcceleration = blowAcceleration;
            BlowRampSeconds = blowRampSeconds;
            AirDrag = airDrag;
            MaxRiseSpeed = maxRiseSpeed;
            MaxFallSpeed = maxFallSpeed;
            FrameBottom = frameBottom;
            FrameTop = frameTop;
            FrameWidth = frameWidth;
            FrameMoveAmplitudeX = frameMoveAmplitudeX;
            FrameMoveAmplitudeY = frameMoveAmplitudeY;
            FrameMovePeriod = frameMovePeriod;
            FeatherHalfWidth = featherHalfWidth;
            FeatherHalfHeight = featherHalfHeight;
            StartY = startY;
            DriftAmplitude = driftAmplitude;
            DriftFrequency = driftFrequency;
            SecondaryDriftAmplitude = secondaryDriftAmplitude;
            SecondaryDriftFrequency = secondaryDriftFrequency;
            TiltDegrees = tiltDegrees;
            TutorialHintDelay = tutorialHintDelay;
            ResultSeconds = resultSeconds;
        }
    }
}
