using System;
using System.Linq;
using Game.Table;

namespace Game.TalismanPractice
{
    public static class StudySettings
    {
        public static string Fingerprint() => MiniGameTableData.Fingerprint("study_settings", "feather_difficulty");
        public static string VisitVariable(PracticeStage stage) => stage == PracticeStage.Feather ? FeatherDifficulty.VisitVariable : "ch2_studyVisits_" + (int)stage;
        public static TalismanPracticeConfig Load(int visit = 1, PracticeStage stage = PracticeStage.Differences)
        {
            var rows = MiniGameTableData.Load<StudySettingsRow>("study_settings");
            var row=MiniGameTableData.SelectLevel(rows,visit,r=>r.Level);
            foreach(var r in rows) { ToConfig(r,PracticeStage.Differences).Validate(); ToConfig(r,PracticeStage.Orders).Validate(); }
            return ToConfig(row,stage);
        }
        private static TalismanPracticeConfig ToConfig(StudySettingsRow row, PracticeStage stage)
        {
            var config=new TalismanPracticeConfig { differenceSeconds=row.DifferenceSeconds, orderSeconds=row.OrderSeconds,
                patienceSeconds=row.PatienceSeconds, customerCount=row.CustomerCount, requestSeconds=row.RequestSeconds,
                reviewSeconds=row.ResultSeconds, tutorialHintDelay=row.TutorialHintDelay,
                stage=stage, tutorial=row.Level==1, difficultyLevel=row.Level, difficultyName=row.Id };
            config.Validate(); return config;
        }
    }
}
