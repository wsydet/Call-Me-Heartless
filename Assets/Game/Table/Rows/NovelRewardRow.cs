using Ember.Table;

namespace Game.Table
{
    /// <summary>剧情奖励的对象、数量、计数倍率与上下限。</summary>
    [EmberTable("novel_rewards")]
    public sealed class NovelRewardRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("id")] public string Id { get; }
        [EmberTableColumn("targetVariableId")] public string TargetVariableId { get; }
        [EmberTableColumn("amount")] public int Amount { get; }
        [EmberTableColumn("multiplierVariableId")] public string MultiplierVariableId { get; }
        [EmberTableColumn("minimum")] public int Minimum { get; }
        [EmberTableColumn("maximum")] public int Maximum { get; }
        [EmberTableColumn("enabled")] public bool Enabled { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelRewardRow(string id, string targetVariableId, int amount,
            string multiplierVariableId, int minimum, int maximum, bool enabled)
        {
            Id = id; TargetVariableId = targetVariableId; Amount = amount;
            MultiplierVariableId = multiplierVariableId; Minimum = minimum;
            Maximum = maximum; Enabled = enabled;
        }
        #endregion
    }
}
