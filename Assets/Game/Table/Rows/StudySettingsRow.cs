using Ember.Table;

namespace Game.Table
{
    [EmberTable("study_settings")]
    public sealed class StudySettingsRow
    {
        [EmberTableKey, EmberTableColumn("id")] public string Id { get; }
        [EmberTableColumn("level")] public int Level { get; }
        [EmberTableColumn("differenceSeconds")] public float DifferenceSeconds { get; }
        [EmberTableColumn("orderSeconds")] public float OrderSeconds { get; }
        [EmberTableColumn("patienceSeconds")] public float PatienceSeconds { get; }
        [EmberTableColumn("customerCount")] public int CustomerCount { get; }
        [EmberTableColumn("requestSeconds")] public float RequestSeconds { get; }
        [EmberTableColumn("resultSeconds")] public float ResultSeconds { get; }
        [EmberTableColumn("tutorialHintDelay")] public float TutorialHintDelay { get; }

        [EmberTableConstructor]
        public StudySettingsRow(string id, float differenceSeconds, float orderSeconds, float patienceSeconds, int customerCount, float requestSeconds, float resultSeconds, float tutorialHintDelay, int level = 1)
        {
            Id = id; Level = level;
            DifferenceSeconds = differenceSeconds;
            OrderSeconds = orderSeconds;
            PatienceSeconds = patienceSeconds;
            CustomerCount = customerCount;
            RequestSeconds = requestSeconds;
            ResultSeconds = resultSeconds;
            TutorialHintDelay = tutorialHintDelay;
        }
    }
}
