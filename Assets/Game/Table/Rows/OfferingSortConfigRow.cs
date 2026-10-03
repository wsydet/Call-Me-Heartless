using Ember.Table;
namespace Game.Table
{
    [EmberTable("offering_sort")]
    public sealed class OfferingSortConfigRow
    {
        [EmberTableKey, EmberTableColumn("id")] public string Id { get; }
        [EmberTableColumn("visitFrom")] public int VisitFrom { get; }
        [EmberTableColumn("visitTo")] public int VisitTo { get; }
        [EmberTableColumn("itemCount")] public int ItemCount { get; }
        [EmberTableColumn("minimumSize")] public int MinimumSize { get; }
        [EmberTableColumn("maximumSize")] public int MaximumSize { get; }
        [EmberTableColumn("timeLimitSeconds")] public int TimeLimitSeconds { get; }
        [EmberTableColumn("smallestFirst")] public bool SmallestFirst { get; }
        [EmberTableColumn("largestLast")] public bool LargestLast { get; }
        [EmberTableColumn("hintEnabled")] public bool HintEnabled { get; }
        [EmberTableColumn("hintAfterNoProgress")] public int HintAfterNoProgress { get; }
        [EmberTableColumn("fastSeconds")] public int FastSeconds { get; }
        [EmberTableColumn("mediumSeconds")] public int MediumSeconds { get; }
        [EmberTableColumn("fastRewardId")] public string FastRewardId { get; }
        [EmberTableColumn("mediumRewardId")] public string MediumRewardId { get; }
        [EmberTableColumn("slowRewardId")] public string SlowRewardId { get; }
        [EmberTableColumn("timeoutRewardId")] public string TimeoutRewardId { get; }
        [EmberTableColumn("hintKey")] public string HintKey { get; }
        [EmberTableColumn("successResultSeconds")] public float SuccessResultSeconds { get; }
        [EmberTableColumn("timeoutResultSeconds")] public float TimeoutResultSeconds { get; }
        [EmberTableConstructor]
        public OfferingSortConfigRow(string id,int visitFrom,int visitTo,int itemCount,int minimumSize,int maximumSize,int timeLimitSeconds,bool smallestFirst,bool largestLast,bool hintEnabled,int hintAfterNoProgress,int fastSeconds,int mediumSeconds,string fastRewardId,string mediumRewardId,string slowRewardId,string timeoutRewardId,string hintKey,float successResultSeconds=1.5f,float timeoutResultSeconds=2)
        { Id=id; VisitFrom=visitFrom; VisitTo=visitTo; ItemCount=itemCount; MinimumSize=minimumSize; MaximumSize=maximumSize; TimeLimitSeconds=timeLimitSeconds; SmallestFirst=smallestFirst; LargestLast=largestLast; HintEnabled=hintEnabled; HintAfterNoProgress=hintAfterNoProgress; FastSeconds=fastSeconds; MediumSeconds=mediumSeconds; FastRewardId=fastRewardId; MediumRewardId=mediumRewardId; SlowRewardId=slowRewardId; TimeoutRewardId=timeoutRewardId; HintKey=hintKey; SuccessResultSeconds=successResultSeconds; TimeoutResultSeconds=timeoutResultSeconds; }
    }
}
