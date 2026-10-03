using System;
using Game.Narrative;
using Game.ShopSelection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public partial class EUIShopSelectionItem
    {
        private Button[] _buttons;
        private TMP_Text[] _names, _prices;
        private Color[] _colors;
        private static string T(string key, string fallback) => NovelLocalization.Runtime("ui.shopSelection." + key, fallback);
        public void Configure(ShopSelectionModel model, Action<int> select, Action cancel)
        {
            _buttons = new[] { Product0, Product1, Product2 };
            _names = new[] { Name0, Name1, Name2 }; _prices = new[] { Price0, Price1, Price2 };
            _colors = new Color[3];
            for (int i = 0; i < 3; i++)
            {
                int index = i; _colors[i] = _buttons[i].GetComponent<Image>().color;
                _buttons[i].onClick.AddListener(() => select(index));
            }
            CancelButton.onClick.AddListener(() => cancel());
            Refresh(model, false);
        }
        public void Refresh(ShopSelectionModel model, bool paused)
        {
            TitleText.text = T("Title", "帮燕于飞买瓶喝的");
            BalanceText.text = string.Format(T("Balance", "零花钱：{0} 元"), model.Balance);
            HintText.text = paused ? T("Paused", "已暂停") : T("Hint", "悬浮查看，点击购买；余额不足的物品不可购买。");
            CancelButton.interactable = !paused && !model.Settled;
            CancelButton.GetComponentInChildren<TMP_Text>().text = T("Cancel", "不购买");
            for (int i = 0; i < 3; i++)
            {
                var product = model.Product(i);
                _names[i].text = NovelLocalization.Runtime(product.textKey, product.name);
                _prices[i].text = string.Format(T("Price", "{0} 元"), product.price);
                _buttons[i].interactable = !paused && model.CanBuy(i);
                var color = _colors[i]; color.a *= _buttons[i].interactable ? 1 : .28f;
                _buttons[i].GetComponent<Image>().color = color;
                _buttons[i].GetComponent<ShopSelectionPointerHandler>().RefreshHighlight();
            }
        }
        public override void OnClose()
        {
            if (_buttons != null) foreach (var button in _buttons) if (button) button.onClick.RemoveAllListeners();
            CancelButton?.onClick.RemoveAllListeners();
            base.OnClose();
        }
        public override void OnDispose() { OnClose(); base.OnDispose(); }
    }
}
