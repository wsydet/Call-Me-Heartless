using System;
using System.Linq;
using Game.Table;

namespace Game.TalismanPractice
{
    public static class FeatherDifficulty
    {
        public const string VisitVariable = "ch2_featherVisits";
        public static FeatherDifficultyRow[] Load()
        {
            var rows = MiniGameTableData.Load<FeatherDifficultyRow>("feather_difficulty").OrderBy(r => r.Level).ToArray();
            if(rows.Length == 0) throw new InvalidOperationException("羽毛难度表为空。");
            for(int i=0;i<rows.Length;i++)
            {
                if(rows[i].Level != i+1 || string.IsNullOrWhiteSpace(rows[i].Label) || (i==0 ? rows[i].TimeLimitSeconds!=0 : rows[i].TimeLimitSeconds<=0))
                    throw new InvalidOperationException("羽毛难度从第1级连续编号；第1级时限为0，其余关卡须限时。");
                ToConfig(rows[i]).Validate();
            }
            return rows;
        }
        public static TalismanPracticeConfig Resolve(int visit)
        {
            if(visit < 1) throw new ArgumentOutOfRangeException(nameof(visit));
            var rows=Load();
            return ToConfig(rows[Math.Min(visit-1, rows.Length-1)]);
        }
        public static TalismanPracticeConfig ToConfig(FeatherDifficultyRow row)
        {
            var config = StudySettings.Load();
            var feather = new TalismanPracticeConfig
            {
                stage = PracticeStage.Feather, tutorial = row.Level == 1,
                difficultyLevel = row.Level,
                difficultyName = row.Label,
                featherSeconds = row.TimeLimitSeconds,
                holdSeconds = row.HoldSeconds,
                gravity = row.Gravity,
                blowAcceleration = row.BlowAcceleration,
                blowRampSeconds = row.BlowRampSeconds,
                airDrag = row.AirDrag,
                maxRiseSpeed = row.MaxRiseSpeed,
                maxFallSpeed = row.MaxFallSpeed,
                frameBottom = row.FrameBottom,
                frameTop = row.FrameTop,
                frameWidth = row.FrameWidth,
                frameMoveAmplitudeX = row.FrameMoveAmplitudeX,
                frameMoveAmplitudeY = row.FrameMoveAmplitudeY,
                frameMovePeriod = row.FrameMovePeriod,
                featherHalfWidth = row.FeatherHalfWidth,
                featherHalfHeight = row.FeatherHalfHeight,
                startY = row.StartY,
                driftAmplitude = row.DriftAmplitude,
                driftFrequency = row.DriftFrequency,
                secondaryDriftAmplitude = row.SecondaryDriftAmplitude,
                secondaryDriftFrequency = row.SecondaryDriftFrequency,
                tiltDegrees = row.TiltDegrees,
                tutorialHintDelay = row.TutorialHintDelay,
                reviewSeconds = row.ResultSeconds,
            };
            feather.requestSeconds = config.requestSeconds;
            return feather;
        }
    }
}
