using System;
namespace Game.PathCleaning
{
    public interface IPathCleaningService : Game.Narrative.INovelStepService {}
    [Serializable] public sealed class PathCleaningRequest { public int Visit; }
    [Serializable] public sealed class PathCleaningResult { public int ElapsedMs; public string Outcome; public string RewardId; }
    public interface IPathCleaningPresenter
    {
        void Open(PathCleaningGame game, Action<string> select, Action<int> clean, Action<int,string> deliver, Action<int,UnityEngine.Vector2,UnityEngine.Vector2> wipe, Action<UnityEngine.Vector2,UnityEngine.Vector2,bool> sweep, Action ready, Action<string> failure);
        void Refresh(PathCleaningGame game);
        void Close();
    }
}
