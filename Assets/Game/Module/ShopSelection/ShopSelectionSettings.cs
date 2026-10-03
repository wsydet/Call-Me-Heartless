using System;
using System.Linq;
using Game.Table;

namespace Game.ShopSelection
{
    public static class ShopSelectionSettings
    {
        public const string PriceVariable = "h_drinkPrice";
        public const string VisitVariable = "ch2_shopVisits";
        public static string Fingerprint() => MiniGameTableData.Fingerprint("shop_settings", "shop_products");
        public static ShopSelectionRequest Load(int balance, int visit = 1)
        {
            var settings=MiniGameTableData.Load<ShopSettingsRow>("shop_settings");
            var rows=MiniGameTableData.Load<ShopProductRow>("shop_products");
            var selected=MiniGameTableData.SelectLevel(settings,visit,r=>r.Level);
            if(rows.Any(r=>!settings.Any(s=>s.Level==r.Level)))throw new InvalidOperationException("商品难度未在购物设置表中声明。");
            foreach(var s in settings)
            {
                if(s.Level==1?s.TimeoutSeconds!=0:s.TimeoutSeconds<=0)throw new InvalidOperationException("购物第一档不限时，后续档须配置正时限。");
                var products=rows.Where(r=>r.Level==s.Level).ToArray();
                if(products.Length!=3 || !new[]{"water","cola","tea"}.All(id=>products.Count(r=>r.ProductId==id)==1))
                    throw new InvalidOperationException("饮品每档须有 water、cola、tea 三个商品。");
                new ShopSelectionModel(Create(balance,s,products));
            }
            var request=Create(balance,selected,rows.Where(r=>r.Level==selected.Level).ToArray());
            new ShopSelectionModel(request); return request;
        }
        private static ShopSelectionRequest Create(int balance,ShopSettingsRow setting,ShopProductRow[] rows) =>
            new ShopSelectionRequest{Balance=balance,TimeoutSeconds=setting.TimeoutSeconds,
                Products=new[]{"water","cola","tea"}.Select(id=>rows.Single(r=>r.ProductId==id))
                    .Select(r=>new ShopProduct{id=r.ProductId,name=r.Name,textKey=r.TextKey,price=r.Price}).ToArray()};
    }
}
