using Ember.Table;

namespace Game.Table
{
    [EmberTable("shop_products")]
    public sealed class ShopProductRow
    {
        [EmberTableKey, EmberTableColumn("id")] public string Id { get; }
        [EmberTableColumn("level")] public int Level { get; }
        [EmberTableColumn("productId")] public string ProductId { get; }
        [EmberTableColumn("name")] public string Name { get; }
        [EmberTableColumn("textKey")] public string TextKey { get; }
        [EmberTableColumn("price")] public int Price { get; }

        [EmberTableConstructor]
        public ShopProductRow(string id, string name, string textKey, int price, int level = 1, string productId = null)
        {
            Id = id; Level = level; ProductId = productId;
            Name = name;
            TextKey = textKey;
            Price = price;
        }
    }
}
