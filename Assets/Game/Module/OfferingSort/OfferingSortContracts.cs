using System;
namespace Game.OfferingSort
{
    public interface IOfferingSortService : Game.Narrative.INovelStepService {}
    [Serializable] public sealed class OfferingSortRequest { public int Visit; }
    [Serializable] public sealed class OfferingSortResult { public int ElapsedMs; public string Outcome; public string RewardId; }
    public interface IOfferingSortPresenter
    {
        void Open(OfferingSortGame game, Action<int,int> swap, Action ready, Action<string> failure);
        void Refresh(OfferingSortGame game);
        void Close();
    }
}
