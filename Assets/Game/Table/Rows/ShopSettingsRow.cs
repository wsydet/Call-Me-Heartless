using Ember.Table;

namespace Game.Table
{
    [EmberTable("shop_settings")]
    public sealed class ShopSettingsRow
    {
        [EmberTableKey, EmberTableColumn("id")] public string Id { get; }
        [EmberTableColumn("level")] public int Level { get; }
        [EmberTableColumn("timeoutSeconds")] public float TimeoutSeconds { get; }

        [EmberTableConstructor]
        public ShopSettingsRow(string id, float timeoutSeconds, int level = 1)
        {
            Id = id; Level = level;
            TimeoutSeconds = timeoutSeconds;
        }
    }
}
