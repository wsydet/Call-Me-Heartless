using System;
using System.Linq;
using Game.Table;
namespace Game.OfferingSort
{
    /// <summary>纯玩法状态；输入只做有效槽位交换。0秒表示没有失败时限。</summary>
    public sealed class OfferingSortGame
    {
        private readonly int[] _order;
        private int _stalled;
        private int _bestInversions;
        public OfferingSortConfigRow Config { get; }
        public System.Collections.Generic.IReadOnlyList<int> Order => _order;
        public double Elapsed { get; private set; }
        public bool HintVisible { get; private set; }
        public bool Settled { get; private set; }
        public OfferingSortResult Result { get; private set; }
        public int Moves { get; private set; }
        public bool IsInteractive { get; internal set; } = true;
        public static void Validate(OfferingSortConfigRow c)
        {
            if(c!=null)foreach(float t in new[]{c.SuccessResultSeconds,c.TimeoutResultSeconds})if(float.IsNaN(t)||float.IsInfinity(t)||t<=0)throw new ArgumentException("结束停留时间必须为正有限数");
            if(c==null || c.VisitFrom<1 || (c.VisitTo!=0 && c.VisitTo<c.VisitFrom)) throw new ArgumentException("难度次数范围无效");
            if(c.ItemCount<2 || c.ItemCount>10 || c.MinimumSize<16 || c.MaximumSize<=c.MinimumSize || c.MaximumSize>240) throw new ArgumentException("祭品数量/尺寸无效：数量2至10，尺寸16至240且最大大于最小");
            if(c.ItemCount-(c.SmallestFirst?1:0)-(c.LargestLast?1:0)<2) throw new ArgumentException("固定首尾后至少要有两件可打乱祭品");
            if(c.TimeLimitSeconds<0 || c.FastSeconds<0 || c.MediumSeconds<c.FastSeconds || (c.HintEnabled && c.HintAfterNoProgress<1)) throw new ArgumentException("时限/奖励档位/提示阈值无效");
            if(new[]{c.FastRewardId,c.MediumRewardId,c.SlowRewardId,c.TimeoutRewardId}.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("奖励ID未配置");
        }
        public OfferingSortGame(OfferingSortConfigRow config,int seed)
        {
            Validate(config);Config=config;_order=Enumerable.Range(0,config.ItemCount).ToArray();
            var random=new Random(seed);int lo=config.SmallestFirst?1:0,hi=config.ItemCount-(config.LargestLast?1:0);
            for(int i=hi-1;i>lo;i--){int j=random.Next(lo,i+1);int v=_order[i];_order[i]=_order[j];_order[j]=v;}
            // Avoid opening with a layout that one accidental exchange can solve.
            int misplaced=0;for(int i=lo;i<hi;i++)if(_order[i]!=i)misplaced++;
            if(hi-lo>=3 && misplaced<3)
            {for(int i=lo;i<hi;i++)_order[i]=i+1;_order[hi-1]=lo;}
            else if(misplaced==0){int v=_order[lo];_order[lo]=_order[lo+1];_order[lo+1]=v;}
            _bestInversions=Inversions();
        }
        public int Inversions(){int n=0;for(int i=0;i<_order.Length;i++)for(int j=i+1;j<_order.Length;j++)if(_order[i]>_order[j])n++;return n;}
        public bool Swap(int from,int to)
        {
            if(!IsInteractive || Settled || from<0 || to<0 || from>=_order.Length || to>=_order.Length || from==to)return false;
            int v=_order[from];_order[from]=_order[to];_order[to]=v;Moves++;
            int after=Inversions();
            if(after<_bestInversions){_bestInversions=after;_stalled=0;}else _stalled++;
            if(after==0){Finish(false);return true;}
            if(Config.HintEnabled && _stalled>=Config.HintAfterNoProgress)HintVisible=true;
            return true;
        }
        public void Tick(double delta)
        {
            if(!IsInteractive || Settled || double.IsNaN(delta) || double.IsInfinity(delta) || delta<0)return;
            Elapsed+=delta;
            if(Config.TimeLimitSeconds>0 && Elapsed>=Config.TimeLimitSeconds){Elapsed=Config.TimeLimitSeconds;Finish(true);}
        }
        private void Finish(bool timeout)
        {
            if(Settled)return;Settled=true;
            string id=timeout?Config.TimeoutRewardId:Elapsed<=Config.FastSeconds?Config.FastRewardId:Elapsed<=Config.MediumSeconds?Config.MediumRewardId:Config.SlowRewardId;
            Result=new OfferingSortResult {ElapsedMs=(int)Math.Min(int.MaxValue,Math.Round(Elapsed*1000)),Outcome=timeout?"timeout":"success",RewardId=id};
        }
    }
}

