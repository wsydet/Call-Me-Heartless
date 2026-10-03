using Ember.Table;
namespace Game.Table {
[EmberTable("path_cleaning_tools")] public sealed class PathCleaningToolRow {
[EmberTableKey, EmberTableColumn("id")] public string Id {get;}
[EmberTableColumn("nameKey")] public string NameKey {get;}
[EmberTableColumn("color")] public string Color {get;}
[EmberTableColumn("spritePath")] public string SpritePath {get;}
[EmberTableConstructor] public PathCleaningToolRow(string id,string nameKey,string color,string spritePath){Id=id;NameKey=nameKey;Color=color;SpritePath=spritePath;}
}}
