using Ember.Table;
namespace Game.Table {
[EmberTable("path_cleaning")] public sealed class PathCleaningConfigRow {
[EmberTableKey, EmberTableColumn("id")] public string Id {get;}
[EmberTableColumn("visitFrom")] public int VisitFrom {get;}
[EmberTableColumn("visitTo")] public int VisitTo {get;}
[EmberTableColumn("itemCount")] public int ItemCount {get;}
[EmberTableColumn("timeLimitSeconds")] public int TimeLimitSeconds {get;}
[EmberTableColumn("typePool")] public string TypePool {get;}
[EmberTableColumn("hintEnabled")] public bool HintEnabled {get;}
[EmberTableColumn("hintAfterErrors")] public int HintAfterErrors {get;}
[EmberTableColumn("hintAfterSeconds")] public int HintAfterSeconds {get;}
[EmberTableColumn("fastSeconds")] public int FastSeconds {get;}
[EmberTableColumn("mediumSeconds")] public int MediumSeconds {get;}
[EmberTableColumn("fastRewardId")] public string FastRewardId {get;}
[EmberTableColumn("mediumRewardId")] public string MediumRewardId {get;}
[EmberTableColumn("slowRewardId")] public string SlowRewardId {get;}
[EmberTableColumn("timeoutRewardId")] public string TimeoutRewardId {get;}
[EmberTableColumn("leafSweepDistance")] public float LeafSweepDistance {get;}
[EmberTableColumn("mudSweepDistance")] public float MudSweepDistance {get;}
[EmberTableColumn("mudSpreadLength")] public float MudSpreadLength {get;}
[EmberTableColumn("mudSpreadOpacity")] public float MudSpreadOpacity {get;}
[EmberTableColumn("mudClearThreshold")] public float MudClearThreshold {get;}
[EmberTableColumn("brushRadius")] public float BrushRadius {get;}
[EmberTableColumn("sweepRadius")] public float SweepRadius {get;}
[EmberTableColumn("successResultSeconds")] public float SuccessResultSeconds {get;}
[EmberTableColumn("timeoutResultSeconds")] public float TimeoutResultSeconds {get;}
[EmberTableConstructor] public PathCleaningConfigRow(string id,int visitFrom,int visitTo,int itemCount,int timeLimitSeconds,string typePool,bool hintEnabled,int hintAfterErrors,int hintAfterSeconds,int fastSeconds,int mediumSeconds,string fastRewardId,string mediumRewardId,string slowRewardId,string timeoutRewardId,float leafSweepDistance=.25f,float mudSweepDistance=.22f,float mudSpreadLength=1f,float mudSpreadOpacity=.9f,float mudClearThreshold=.92f,float brushRadius=.13f,float sweepRadius=.11f,float successResultSeconds=.9f,float timeoutResultSeconds=2){Id=id;VisitFrom=visitFrom;VisitTo=visitTo;ItemCount=itemCount;TimeLimitSeconds=timeLimitSeconds;TypePool=typePool;HintEnabled=hintEnabled;HintAfterErrors=hintAfterErrors;HintAfterSeconds=hintAfterSeconds;FastSeconds=fastSeconds;MediumSeconds=mediumSeconds;FastRewardId=fastRewardId;MediumRewardId=mediumRewardId;SlowRewardId=slowRewardId;TimeoutRewardId=timeoutRewardId;LeafSweepDistance=leafSweepDistance;MudSweepDistance=mudSweepDistance;MudSpreadLength=mudSpreadLength;MudSpreadOpacity=mudSpreadOpacity;MudClearThreshold=mudClearThreshold;BrushRadius=brushRadius;SweepRadius=sweepRadius;SuccessResultSeconds=successResultSeconds;TimeoutResultSeconds=timeoutResultSeconds;}
}}
