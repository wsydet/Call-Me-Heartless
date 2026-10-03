using System;
using System.Collections.Generic;

namespace Game.ShopSelection
{
    public sealed class ShopSelectionModel
    {
        private readonly ShopProduct[] _products;
        private readonly bool _untimed;
        public int Balance { get; }
        public int Count => _products.Length;
        public double Remaining { get; private set; }
        public string Result { get; private set; }
        public bool Settled => Result != null;
        public ShopProduct Product(int index) => _products[index];

        public ShopSelectionModel(ShopSelectionRequest request)
        {
            if (request == null || request.Balance < 0 || request.Products == null || request.Products.Length != 3 ||
                double.IsNaN(request.TimeoutSeconds) || double.IsInfinity(request.TimeoutSeconds) || request.TimeoutSeconds < 0)
                throw new ArgumentException("购物请求参数无效");
            Balance = request.Balance; Remaining = request.TimeoutSeconds;
            _untimed = request.TimeoutSeconds == 0;
            _products = new ShopProduct[request.Products.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _products.Length; i++)
            {
                var p = request.Products[i];
                if (p == null || string.IsNullOrWhiteSpace(p.id) || p.id == "cancel" || !ids.Add(p.id) ||
                    string.IsNullOrWhiteSpace(p.name) || string.IsNullOrWhiteSpace(p.textKey) || p.price <= 0)
                    throw new ArgumentException("商品定义无效");
                _products[i] = new ShopProduct { id = p.id, name = p.name, textKey = p.textKey, price = p.price };
            }
        }

        public bool CanBuy(int index) => !Settled && index >= 0 && index < Count && Balance >= _products[index].price;
        public bool Select(int index, bool paused)
        {
            if (paused || !CanBuy(index)) return false;
            Result = _products[index].id;
            return true;
        }
        public void Cancel(bool paused) { if (!paused && !Settled) Result = "cancel"; }
        public void Tick(double delta, bool paused)
        {
            if (double.IsNaN(delta) || double.IsInfinity(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
            if (paused || Settled || _untimed) return;
            Remaining = Math.Max(0, Remaining - delta);
            if (Remaining <= 0) Cancel(false);
        }
    }
}
