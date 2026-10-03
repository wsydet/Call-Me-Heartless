using System;
using Ember.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.ShopSelection.Tests
{
    public sealed class ShopSelectionTests
    {
        public static ShopSelectionRequest Request(int balance = 25) => new ShopSelectionRequest {
            Balance = balance, TimeoutSeconds = 300, Products = new[] {
                new ShopProduct { id = "water", name = "矿泉水", textKey = "water", price = 5 },
                new ShopProduct { id = "cola", name = "柠檬可乐", textKey = "cola", price = 10 },
                new ShopProduct { id = "tea", name = "顶级龙井", textKey = "tea", price = 100 }
            }
        };
        [TestCase(0,0)] [TestCase(4,0)] [TestCase(5,1)] [TestCase(9,1)]
        [TestCase(10,2)] [TestCase(99,2)] [TestCase(100,3)]
        public void BalanceControlsAvailableProducts(int balance, int expected)
        {
            var model = new ShopSelectionModel(Request(balance));
            int count = 0; for (int i = 0; i < 3; i++) if (model.CanBuy(i)) count++;
            Assert.That(count, Is.EqualTo(expected));
        }
        [Test] public void ChoiceSettlesOnceWithoutMutatingMoney()
        {
            var model = new ShopSelectionModel(Request());
            Assert.False(model.Select(2, false)); Assert.False(model.Select(0, true));
            Assert.True(model.Select(1, false)); Assert.False(model.Select(0, false));
            model.Cancel(false); model.Tick(400, false);
            Assert.That(model.Result, Is.EqualTo("cola")); Assert.That(model.Balance, Is.EqualTo(25));
        }
        [Test] public void PauseFreezesTimeoutAndCancel()
        {
            var model = new ShopSelectionModel(Request()); model.Tick(299, false);
            model.Tick(500, true); model.Cancel(true); Assert.False(model.Settled);
            model.Tick(1, false); Assert.That(model.Result, Is.EqualTo("cancel"));
        }
        [Test] public void InvalidCatalogAndNegativeBalanceFail()
        {
            Assert.Throws<ArgumentException>(() => new ShopSelectionModel(Request(-1)));
            var r = Request(); r.Products[2].id = "cola"; Assert.Throws<ArgumentException>(() => new ShopSelectionModel(r));
            r = Request(); r.TimeoutSeconds = double.NaN; Assert.Throws<ArgumentException>(() => new ShopSelectionModel(r));
        }
        private sealed class View : IShopSelectionPresenter
        {
            public Action<int> Select; public Action Cancel; public int Closes;
            public void Open(ShopSelectionModel model, Action<int> select, Action cancel, Action<string> fail) { Select = select; Cancel = cancel; }
            public void Refresh(ShopSelectionModel model, bool paused) { }
            public void Close() { Closes++; }
        }
        [Test] public void ModuleRejectsLateAndDuplicateCallbacks()
        {
            var module = new ShopSelectionModule(); var view = new View();
            var previous = EmberServiceLocator.TryResolve<IShopSelectionPresenter>();
            EmberServiceLocator.Register<IShopSelectionPresenter>(view);
            try
            {
                int completed = 0, errors = 0; string result = null;
                module.Invoke("cmh.h002.drink_selection", "first", Request(), v => { completed++; result = v; }, _ => errors++);
                var late = view.Select; module.Abort("first"); late(0); Assert.That(completed, Is.Zero);
                module.Invoke("cmh.h002.drink_selection", "second", Request(), v => { completed++; result = v; }, _ => errors++);
                late(0); Assert.That(completed, Is.Zero);
                var choose = view.Select; choose(2); Assert.That(completed, Is.Zero);
                choose(1); choose(0); Assert.That(completed, Is.EqualTo(1)); Assert.That(result, Is.EqualTo("cola"));
                Assert.That(errors, Is.Zero); Assert.That(view.Closes, Is.EqualTo(2));
                module.Invoke("cmh.h002.drink_selection", "third", Request(), v => { completed++; result = v; }, _ => errors++);
                view.Cancel(); Assert.That(result, Is.EqualTo("cancel")); Assert.That(completed, Is.EqualTo(2));
            }
            finally { module.Abort("third"); EmberServiceLocator.Unregister<IShopSelectionPresenter>(); if (previous != null) EmberServiceLocator.Register<IShopSelectionPresenter>(previous); }
        }
    }
}
