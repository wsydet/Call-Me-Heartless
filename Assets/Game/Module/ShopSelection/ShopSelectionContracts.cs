using System;

namespace Game.ShopSelection
{
    [Serializable]
    public sealed class ShopProduct
    {
        public string id, name, textKey;
        public int price;
    }

    public sealed class ShopSelectionRequest
    {
        public int Balance;
        public double TimeoutSeconds;
        public ShopProduct[] Products;
    }

    public interface IShopSelectionService
    {
        void Invoke(string requestKey, string executionId, ShopSelectionRequest request, Action<string> complete, Action<string> fail);
        void Abort(string executionId);
    }

    public interface IShopSelectionPresenter
    {
        void Open(ShopSelectionModel model, Action<int> select, Action cancel, Action<string> fail);
        void Refresh(ShopSelectionModel model, bool paused);
        void Close();
    }
}
