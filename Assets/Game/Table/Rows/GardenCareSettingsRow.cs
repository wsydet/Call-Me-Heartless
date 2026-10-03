using Ember.Table;

namespace Game.Table
{
    [EmberTable("garden_care")]
    public sealed class GardenCareSettingsRow
    {
        [EmberTableKey, EmberTableColumn("id")] public string Id { get; }
        [EmberTableColumn("level")] public int Level { get; }
        [EmberTableColumn("cropCount")] public int CropCount { get; }
        [EmberTableColumn("weedCount")] public int WeedCount { get; }
        [EmberTableColumn("insectCount")] public int InsectCount { get; }
        [EmberTableColumn("timeLimitSeconds")] public float TimeLimitSeconds { get; }
        [EmberTableColumn("columns")] public int Columns { get; }
        [EmberTableColumn("rows")] public int Rows { get; }
        [EmberTableColumn("resultSeconds")] public float ResultSeconds { get; }
        [EmberTableColumn("tutorialHintDelay")] public float TutorialHintDelay { get; }
        [EmberTableColumn("requestSeconds")] public float RequestSeconds { get; }

        [EmberTableConstructor]
        public GardenCareSettingsRow(string id, int cropCount, int weedCount, int insectCount, float timeLimitSeconds, int columns, int rows, float resultSeconds, float tutorialHintDelay, float requestSeconds, int level = 1)
        {
            Id = id; Level = level;
            CropCount = cropCount;
            WeedCount = weedCount;
            InsectCount = insectCount;
            TimeLimitSeconds = timeLimitSeconds;
            Columns = columns;
            Rows = rows;
            ResultSeconds = resultSeconds;
            TutorialHintDelay = tutorialHintDelay;
            RequestSeconds = requestSeconds;
        }
    }
}
