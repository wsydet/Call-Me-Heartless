using Ember.Table;
namespace Game.Table {
[EmberTable("path_cleaning_types")] public sealed class PathCleaningTypeRow {
[EmberTableKey, EmberTableColumn("id")] public string Id {get;}
[EmberTableColumn("pool")] public string Pool {get;}
[EmberTableColumn("toolId")] public string ToolId {get;}
[EmberTableColumn("weight")] public int Weight {get;}
[EmberTableColumn("color")] public string Color {get;}
[EmberTableColumn("size")] public int Size {get;}
[EmberTableColumn("spritePath")] public string SpritePath {get;}
[EmberTableColumn("hintKey")] public string HintKey {get;}
[EmberTableConstructor] public PathCleaningTypeRow(string id,string pool,string toolId,int weight,string color,int size,string spritePath,string hintKey){Id=id;Pool=pool;ToolId=toolId;Weight=weight;Color=color;Size=size;SpritePath=spritePath;HintKey=hintKey;}
}}
