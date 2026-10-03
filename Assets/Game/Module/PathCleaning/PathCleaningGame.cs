using System;
using System.Linq;
using Game.Table;
namespace Game.PathCleaning
{
    /// <summary>工具匹配、计时和教程的纯状态；0秒表示不限时。</summary>
    public sealed class PathCleaningGame
    {
        // Keep a 128px source at native density inside a padded 256px canvas.
        // Bounds never resize during a stroke, so existing artwork cannot jump or blur.
        public const int MudSourceResolution = 128;
        public const int MudResolution = MudSourceResolution * 2;
        public const float MudDensityThreshold = .035f;
        public float MudClearThreshold => Config.MudClearThreshold;
        public float BrushRadius => Config.BrushRadius;
        public sealed class Spot
        {
            public PathCleaningTypeRow Type {get;internal set;}
            public bool Cleared {get;internal set;}
            public int Cell {get;internal set;}
            public UnityEngine.Vector2 Position {get;internal set;}
            public float LeafRotation {get;internal set;}
            internal float LeafStrokeTravel;
            internal float MudStrokeTravel;
            internal UnityEngine.Vector2 MudPendingPush;
            public UnityEngine.Rect MudBounds {get;internal set;}=new UnityEngine.Rect(-.5f,-.5f,2,2);
            public UnityEngine.Vector2 LeafFootprint {get;internal set;}=new UnityEngine.Vector2(.04f,.06f);
            internal bool[] MudPotential;
            internal float[] MudDensity, SmearDensity, SmearPressure;
            internal bool MudStrokePrepared;
            internal int[] SmearSources;
            public float MudOpacity(int pixel) => IsDirtyPixel(pixel) ? MudDensity[pixel] : 0;
            internal int[] MudSources;
            public int MudSourcePixel(int pixel) => MudSources==null?pixel:MudSources[pixel];
            public UnityEngine.Vector2 MudFootprint {get;internal set;}=new UnityEngine.Vector2(.08f,.15f);
            internal bool[] DirtyPixels;
            internal int OriginalPixels, RemainingPixels;
            public int MudRevision {get;internal set;}
            public float MudAspectRatio {get;internal set;}=1;
            public float WipedFraction => OriginalPixels == 0 ? 0 : 1f - (float)RemainingPixels / OriginalPixels;
            public bool IsDirtyPixel(int pixel) => DirtyPixels != null && pixel >= 0 && pixel < DirtyPixels.Length && DirtyPixels[pixel];
        }
        public PathCleaningConfigRow Config {get;}
        public System.Collections.Generic.IReadOnlyList<Spot> Spots {get;}
        public System.Collections.Generic.IReadOnlyList<PathCleaningToolRow> Tools {get;}
        public string SelectedTool {get;private set;}
        public string HintKey {get;private set;}
        public double Elapsed {get;private set;}
        public double ErrorUntil {get;private set;}
        public int ErrorSpot {get;private set;}=-1;
        public int ClearedCount {get;private set;}
        public int SmearSpot {get;private set;}=-1;
        public double SmearUntil {get;private set;}
        public bool Settled {get;private set;}
        public PathCleaningResult Result {get;private set;}
        private int _errors;
        private double _lastProgress;
        private float _boardAspect=1;
        public UnityEngine.Rect LeafCollection {get;private set;}=new UnityEngine.Rect(.075f,.005f,.15f,.17f);
        public static void Validate(PathCleaningConfigRow c,PathCleaningTypeRow[] types,PathCleaningToolRow[] tools)
        {
            if(c==null||c.VisitFrom<1||(c.VisitTo!=0&&c.VisitTo<c.VisitFrom)||c.ItemCount<1||c.ItemCount>20||c.TimeLimitSeconds<0)throw new ArgumentException("打扫难度次数、数量(1至20)或时限无效");
            if(!PositiveFinite(c.MudClearThreshold,1)||!PositiveFinite(c.BrushRadius,1)||!PositiveFinite(c.SweepRadius,1)||!PositiveFinite(c.SuccessResultSeconds,60)||!PositiveFinite(c.TimeoutResultSeconds,60))throw new ArgumentException("清洁判定、工具范围或收尾时间无效");
            if(c.FastSeconds<0||c.MediumSeconds<c.FastSeconds||(c.HintEnabled&&(c.HintAfterErrors<1||c.HintAfterSeconds<1)))throw new ArgumentException("打扫奖励或教程阈值无效");
            if(!PositiveFinite(c.LeafSweepDistance,1)||!PositiveFinite(c.MudSweepDistance,1)||!PositiveFinite(c.MudSpreadLength,1.5f)||!PositiveFinite(c.MudSpreadOpacity,1))throw new ArgumentException("扫动距离须在(0,1]，污渍扩散长度须在(0,1.5]，不透明度须在(0,1]");
            if(new[]{c.FastRewardId,c.MediumRewardId,c.SlowRewardId,c.TimeoutRewardId}.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("奖励ID缺失");
            if(types==null||tools==null||types.GroupBy(t=>t.Id).Any(g=>g.Count()!=1)||tools.GroupBy(t=>t.Id).Any(g=>g.Count()!=1))throw new ArgumentException("类型/工具重复或缺失");
            var pool=types.Where(t=>t.Pool==c.TypePool).ToArray();
            if(pool.Length==0||pool.Sum(t=>(long)t.Weight)>int.MaxValue)throw new ArgumentException("类型池为空或总权重溢出");
            foreach(var t in types){if(string.IsNullOrWhiteSpace(t.Id)||t.Weight<1||t.Size<16||t.Size>96||!tools.Any(x=>x.Id==t.ToolId)||string.IsNullOrWhiteSpace(t.HintKey)||!UnityEngine.ColorUtility.TryParseHtmlString(t.Color,out _))throw new ArgumentException("污渍类型配置无效："+t.Id);}
            foreach(var t in tools)if(string.IsNullOrWhiteSpace(t.Id)||string.IsNullOrWhiteSpace(t.NameKey)||!UnityEngine.ColorUtility.TryParseHtmlString(t.Color,out _))throw new ArgumentException("工具配置无效："+t.Id);
            if(pool.Select(t=>t.ToolId).Distinct().Count()>6)throw new ArgumentException("每局工具最多6种");
        }
        private static bool PositiveFinite(float value,float maximum)=>!float.IsNaN(value)&&!float.IsInfinity(value)&&value>0&&value<=maximum;
        public PathCleaningGame(PathCleaningConfigRow config,PathCleaningTypeRow[] types,PathCleaningToolRow[] tools,int seed)
        {
            Validate(config,types,tools);Config=config;var pool=types.Where(t=>t.Pool==config.TypePool).ToArray();
            Tools=tools.Where(t=>pool.Any(p=>p.ToolId==t.Id)).ToArray();var random=new Random(seed);var cells=Enumerable.Range(0,20).ToArray();
            for(int i=cells.Length-1;i>0;i--){int j=random.Next(i+1);int v=cells[i];cells[i]=cells[j];cells[j]=v;}
            var spots=new Spot[config.ItemCount];var bag=AllocateTypes(pool,config.ItemCount,random);
            for(int i=0;i<spots.Length;i++)spots[i]=new Spot{Type=bag[i],Cell=cells[i],Position=new UnityEngine.Vector2((cells[i]%4+.5f)/4,.23f+(cells[i]/4+.5f)/5*.74f)};
            Spots=spots;
            foreach(var spot in spots)
            {
                if(spot.Type.Id!="mud")continue;
                var mask=new bool[MudSourceResolution*MudSourceResolution];
                var density=new float[mask.Length];
                for(int y=0;y<MudSourceResolution;y++)for(int x=0;x<MudSourceResolution;x++)
                {
                    float u=(x+.5f)/MudSourceResolution,v=(y+.5f)/MudSourceResolution;
                    float dx=(u-.5f)/.43f,dy=(v-.5f)/.37f;
                    float edge=1+.12f*UnityEngine.Mathf.Sin(u*23+spot.Cell)+.08f*UnityEngine.Mathf.Cos(v*27+u*11);
                    float alpha=UnityEngine.Mathf.SmoothStep(0,1,UnityEngine.Mathf.Clamp01((edge-dx*dx-dy*dy)/.18f));
                    int pixel=y*MudSourceResolution+x;density[pixel]=alpha;mask[pixel]=alpha>MudDensityThreshold;
                }
                InitializeMud(spot,mask,density);
            }
        }
        // 总数足够时先保证每类出现，再按表中权重分配余量；只随机摆放，不逐个抽类型。
        private static PathCleaningTypeRow[] AllocateTypes(PathCleaningTypeRow[] pool,int count,Random random)
        {
            var bag=new System.Collections.Generic.List<PathCleaningTypeRow>(count);
            int total=pool.Sum(t=>t.Weight);
            if(count<pool.Length)
            {
                // 总数不足以覆盖类型池时，仍按权重抽取，允许小规模单物品关卡。
                for(int i=0;i<count;i++)
                {
                    int roll=random.Next(total);
                    foreach(var type in pool){roll-=type.Weight;if(roll<0){bag.Add(type);break;}}
                }
            }
            else
            {
                int remaining=count-pool.Length;var counts=new int[pool.Length];var remainders=new long[pool.Length];
                int assigned=0;
                for(int i=0;i<pool.Length;i++)
                {
                    long weighted=(long)remaining*pool[i].Weight;
                    counts[i]=1+(int)(weighted/total);remainders[i]=weighted%total;assigned+=counts[i];
                }
                foreach(int i in Enumerable.Range(0,pool.Length).OrderByDescending(i=>remainders[i]).ThenBy(i=>i).Take(count-assigned))counts[i]++;
                for(int i=0;i<pool.Length;i++)for(int j=0;j<counts[i];j++)bag.Add(pool[i]);
            }
            for(int i=bag.Count-1;i>0;i--){int j=random.Next(i+1);var value=bag[i];bag[i]=bag[j];bag[j]=value;}
            return bag.ToArray();
        }
        public void ConfigureMudMask(int index,System.Collections.Generic.IReadOnlyList<bool> mask,float aspectRatio=1)
        {
            if(index<0||index>=Spots.Count||Spots[index].Type.Id!="mud")throw new ArgumentException("只有泥渍使用擦除遮罩");
            var spot=Spots[index];
            if(Settled||Elapsed>0||SelectedTool!=null||spot.WipedFraction>0)throw new InvalidOperationException("擦除遮罩只能在开始前配置");
            if(mask==null||mask.Count!=MudSourceResolution*MudSourceResolution||aspectRatio<=0||float.IsNaN(aspectRatio)||float.IsInfinity(aspectRatio))throw new ArgumentException("泥渍遮罩尺寸或比例无效");
            int count=0;var copy=new bool[mask.Count];for(int i=0;i<copy.Length;i++){copy[i]=mask[i];if(copy[i])count++;}
            if(count==0)throw new ArgumentException("泥渍图片完全透明，没有可擦除内容");
            InitializeMud(spot,copy,null);spot.MudAspectRatio=aspectRatio;spot.MudRevision++;
        }
        private static void InitializeMud(Spot spot,bool[] mask,float[] density)
        {
            int count=MudResolution*MudResolution;
            spot.DirtyPixels=new bool[count];spot.MudPotential=new bool[count];
            spot.MudDensity=new float[count];spot.SmearDensity=new float[count];spot.SmearPressure=new float[count];
            spot.MudSources=new int[count];spot.SmearSources=new int[count];
            spot.OriginalPixels=spot.RemainingPixels=0;
            int padding=MudSourceResolution/2;
            for(int y=0;y<MudSourceResolution;y++)for(int x=0;x<MudSourceResolution;x++)
            {
                int source=y*MudSourceResolution+x,pixel=(y+padding)*MudResolution+x+padding;
                spot.MudSources[pixel]=source;
                if(!mask[source])continue;
                spot.DirtyPixels[pixel]=spot.MudPotential[pixel]=true;
                spot.MudDensity[pixel]=density==null?1:density[source];
                spot.OriginalPixels++;spot.RemainingPixels++;
            }
        }
        public bool Select(string id){if(Settled||!Tools.Any(t=>t.Id==id))return false;SelectedTool=id;return true;}
        public static string CollectionTarget(Spot spot) => spot.Type.Id == "trash" ? "trashBin" : spot.Type.Id == "leaves" ? "leafPile" : null;
        public bool Clean(int index, string destination = null)
        {
            if(Settled||index<0||index>=Spots.Count||Spots[index].Cleared)return false;
            var spot=Spots[index];
            if(SelectedTool!=spot.Type.ToolId || CollectionTarget(spot)!=destination || (spot.Type.Id=="mud"||spot.Type.Id=="leaves"))return Reject(index);
            return Clear(spot);
        }
        private bool Reject(int index)
        {
            _errors++;ErrorSpot=index;ErrorUntil=Elapsed+.55;
            if(Config.HintEnabled&&_errors>=Config.HintAfterErrors)HintKey=Spots[index].Type.HintKey;
            return false;
        }
        private void Progress()
        { _errors=0;_lastProgress=Elapsed;HintKey=null;ErrorSpot=-1; }
        public bool Wipe(int index, UnityEngine.Vector2 from, UnityEngine.Vector2 to)
        {
            if(Settled||index<0||index>=Spots.Count||Spots[index].Cleared)return false;
            var spot=Spots[index];
            if(spot.Type.Id!="mud"||SelectedTool!=spot.Type.ToolId)return Reject(index);
            if(float.IsNaN(from.x)||float.IsNaN(from.y)||float.IsNaN(to.x)||float.IsNaN(to.y)||
                float.IsInfinity(from.x)||float.IsInfinity(from.y)||float.IsInfinity(to.x)||float.IsInfinity(to.y)||
                Math.Abs(from.x)>10000||Math.Abs(from.y)>10000||Math.Abs(to.x)>10000||Math.Abs(to.y)>10000)return false;
            float aspect=spot.MudAspectRatio*spot.MudBounds.width/spot.MudBounds.height;
            from.x*=aspect;to.x*=aspect;
            float radius=BrushRadius*Math.Min(1,aspect)/spot.MudBounds.height;
            var delta=to-from;float length=delta.sqrMagnitude;bool changed=false;
            for(int y=0;y<MudResolution;y++)for(int x=0;x<MudResolution;x++)
            {
                int pixel=y*MudResolution+x;if(!spot.DirtyPixels[pixel])continue;
                var position=new UnityEngine.Vector2((x+.5f)/MudResolution*aspect,(y+.5f)/MudResolution);
                float t=length<=.000001f?0:UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(position-from,delta)/length);
                if((position-(from+delta*t)).sqrMagnitude>radius*radius)continue;
                spot.DirtyPixels[pixel]=false;spot.MudDensity[pixel]=0;spot.RemainingPixels--;changed=true;
            }
            if(!changed)return false;
            spot.MudRevision++;Progress();
            if(spot.WipedFraction>=MudClearThreshold)Clear(spot);
            return true;
        }
        public void ConfigureMudFootprint(int index,UnityEngine.Vector2 size)
        {
            if(Settled||SelectedTool!=null||Elapsed>0)throw new InvalidOperationException("污渍位置只能在开始前配置");
            if(index<0||index>=Spots.Count||Spots[index].Type.Id!="mud"||size.x<=0||size.y<=0)throw new ArgumentException("污渍尺寸无效");
            Spots[index].MudFootprint=size;
        }
        public void ConfigureSweepArea(UnityEngine.Rect collection,float boardAspect)
        {
            if(Settled||SelectedTool!=null||Elapsed>0)throw new InvalidOperationException("扫叶区域只能在开始前配置");
            if(boardAspect<=0||float.IsNaN(boardAspect)||float.IsInfinity(boardAspect)||collection.width<=0||collection.height<=0)throw new ArgumentException("扫叶区域尺寸无效");
            LeafCollection=collection;_boardAspect=boardAspect;
        }
        public bool Sweep(UnityEngine.Vector2 from,UnityEngine.Vector2 to,bool beginStroke=false)
        {
            if(Settled||float.IsNaN(from.x)||float.IsNaN(from.y)||float.IsNaN(to.x)||float.IsNaN(to.y)||
                float.IsInfinity(from.x)||float.IsInfinity(from.y)||float.IsInfinity(to.x)||float.IsInfinity(to.y)||
                Math.Abs(from.x)>4||Math.Abs(from.y)>4||Math.Abs(to.x)>4||Math.Abs(to.y)>4)return false;
            if(beginStroke)foreach(var spot in Spots){spot.LeafStrokeTravel=0;spot.MudStrokeTravel=0;spot.MudPendingPush=UnityEngine.Vector2.zero;spot.MudStrokePrepared=false;}
            from.x*=_boardAspect;to.x*=_boardAspect;var delta=to-from;float distance=delta.magnitude;
            if(distance<.0001f)return false;var direction=delta/distance;float radius=Config.SweepRadius;bool changed=false;bool smeared=false;
            foreach(var spot in Spots)
            {
                if(spot.Type.Id!="leaves"||SelectedTool!=spot.Type.ToolId)continue;
                var position=new UnityEngine.Vector2(spot.Position.x*_boardAspect,spot.Position.y);
                float t=UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(position-from,delta)/(distance*distance));
                if((position-(from+delta*t)).sqrMagnitude>radius*radius)continue;
                float budget=Config.LeafSweepDistance-spot.LeafStrokeTravel;if(budget<=.00001f)continue;
                float push=Math.Min(budget,distance*(1-t));if(push<=.00001f)continue;
                position+=direction*push;spot.LeafStrokeTravel+=push;
                spot.Position=new UnityEngine.Vector2(UnityEngine.Mathf.Clamp01(position.x/_boardAspect),UnityEngine.Mathf.Clamp01(position.y));
                spot.LeafRotation=UnityEngine.Mathf.Atan2(direction.y,direction.x)*UnityEngine.Mathf.Rad2Deg*.12f;
                changed=true;bool inside=LeafCollection.Contains(spot.Position-spot.LeafFootprint*.5f)&&LeafCollection.Contains(spot.Position+spot.LeafFootprint*.5f);
                if(inside!=spot.Cleared){spot.Cleared=inside;ClearedCount+=inside?1:-1;}
            }
            if(changed)Progress();
            if(SelectedTool=="broom")for(int index=0;index<Spots.Count;index++)
            {
                var spot=Spots[index];if(spot.Cleared||spot.Type.Id!="mud"||spot.MudStrokeTravel>=Config.MudSweepDistance)continue;
                if(!Smear(spot,from,to,radius))continue;
                SmearSpot=index;SmearUntil=Elapsed+1;smeared=true;
            }
            if(ClearedCount==Spots.Count)Finish(false);return changed||smeared;
        }
        public void ConfigureLeafFootprint(int index,UnityEngine.Vector2 size)
        {
            if(Settled||SelectedTool!=null||Elapsed>0)throw new InvalidOperationException("落叶尺寸只能在开始前配置");
            if(index<0||index>=Spots.Count||Spots[index].Type.Id!="leaves"||size.x<=0||size.y<=0)throw new ArgumentException("落叶尺寸无效");
            Spots[index].LeafFootprint=size;
        }
        private bool Smear(Spot spot,UnityEngine.Vector2 from,UnityEngine.Vector2 to,float radius)
        {
            var delta=to-from;float length=delta.magnitude;
            float push=Math.Min(length*.65f,Config.MudSweepDistance-spot.MudStrokeTravel);
            if(push<=0)return false;
            var bounds=spot.MudBounds;float pixelsPerUnit=MudResolution/bounds.width;
            bool contact=false,changed=false;
            // Capture once per press. Re-sampling the already weakened tail on every
            // pointer event made a real, finely segmented drag almost invisible.
            if(!spot.MudStrokePrepared)
            {
                Array.Copy(spot.MudDensity,spot.SmearDensity,spot.MudDensity.Length);
                Array.Copy(spot.MudSources,spot.SmearSources,spot.MudSources.Length);
                Array.Clear(spot.SmearPressure,0,spot.SmearPressure.Length);
                spot.MudStrokePrepared=true;
            }
            for(int y=0;y<MudResolution;y++)for(int x=0;x<MudResolution;x++)
            {
                int old=y*MudResolution+x;if(spot.SmearDensity[old]<=MudDensityThreshold)continue;
                var uv=new UnityEngine.Vector2(bounds.xMin+(x+.5f)/pixelsPerUnit,bounds.yMin+(y+.5f)/pixelsPerUnit);
                var point=spot.Position+new UnityEngine.Vector2((uv.x-.5f)*spot.MudFootprint.x,(uv.y-.5f)*spot.MudFootprint.y);point.x*=_boardAspect;
                float t=UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(point-from,delta)/(length*length));
                float distance=(point-(from+delta*t)).magnitude;
                if(distance>=radius)continue;
                contact=true;
                float pressure=1-UnityEngine.Mathf.SmoothStep(0,1,UnityEngine.Mathf.InverseLerp(.45f,1,distance/radius));
                spot.SmearPressure[old]=Math.Max(spot.SmearPressure[old],pressure);
            }
            if(!contact)return false;
            spot.MudStrokeTravel+=push;
            spot.MudPendingPush+=delta/length*push;
            var shift=new UnityEngine.Vector2(spot.MudPendingPush.x/_boardAspect/spot.MudFootprint.x,spot.MudPendingPush.y/spot.MudFootprint.y);
            shift=UnityEngine.Vector2.ClampMagnitude(shift,Config.MudSpreadLength);
            float shiftPixels=shift.magnitude*pixelsPerUnit;
            if(shiftPixels<.75f)return false;
            var direction=spot.MudPendingPush.normalized;
            for(int y=0;y<MudResolution;y++)for(int x=0;x<MudResolution;x++)
            {
                int old=y*MudResolution+x;float density=spot.SmearDensity[old],pressure=spot.SmearPressure[old];
                if(density<=MudDensityThreshold||pressure<=0)continue;
                var uv=new UnityEngine.Vector2(bounds.xMin+(x+.5f)/pixelsPerUnit,bounds.yMin+(y+.5f)/pixelsPerUnit);
                float across=-uv.x*direction.y+uv.y*direction.x;
                float bristle=.90f+.06f*UnityEngine.Mathf.Sin(across*23+spot.Cell)+.04f*UnityEngine.Mathf.Sin(across*61);
                float strength=density*Config.MudSpreadOpacity*pressure;
                if(strength<=MudDensityThreshold)continue;
                var travel=shift*(pressure*bristle);
                // A bounded number of overlapping stamps keeps fast swipes affordable.
                int steps=Math.Min(12,(int)Math.Ceiling(shiftPixels*pressure*bristle));
                for(int step=1;step<=steps;step++)
                {
                    float progress=(float)step/steps;
                    var target=uv+travel*progress;
                    // Fade before the capacity boundary instead of hitting a rectangular wall.
                    float edge=Math.Min(Math.Min(target.x-bounds.xMin,bounds.xMax-target.x),Math.Min(target.y-bounds.yMin,bounds.yMax-target.y));
                    // Keep a readable body of displaced mud, then soften only the end.
                    float tail=1-UnityEngine.Mathf.SmoothStep(0,1,UnityEngine.Mathf.InverseLerp(.4f,1,progress));
                    float opacity=strength*tail*UnityEngine.Mathf.Clamp01(edge/.12f);
                    if(opacity<=MudDensityThreshold)continue;
                    float fx=(target.x-bounds.xMin)*pixelsPerUnit-.5f,fy=(target.y-bounds.yMin)*pixelsPerUnit-.5f;
                    int ix=(int)Math.Floor(fx),iy=(int)Math.Floor(fy);
                    // A soft coverage kernel keeps fractional-pixel splats visible in the
                    // small UI, without repeatedly weakening them at four cell corners.
                    for(int oy=0;oy<=1;oy++)for(int ox=0;ox<=1;ox++)
                    {
                        int px=ix+ox,py=iy+oy;if(px<0||py<0||px>=MudResolution||py>=MudResolution)continue;
                        float weight=(ox==0?1-(fx-ix):fx-ix)*(oy==0?1-(fy-iy):fy-iy);
                        int edgePixels=Math.Min(Math.Min(px,MudResolution-1-px),Math.Min(py,MudResolution-1-py));
                        float value=opacity*UnityEngine.Mathf.Sqrt(weight)*UnityEngine.Mathf.Clamp01(edgePixels/(.08f*pixelsPerUnit));int pixel=py*MudResolution+px;
                        if(value<=MudDensityThreshold||value<=spot.MudDensity[pixel])continue;
                        spot.MudDensity[pixel]=value;spot.MudSources[pixel]=spot.SmearSources[old];
                        if(!spot.DirtyPixels[pixel]){spot.DirtyPixels[pixel]=true;spot.RemainingPixels++;}
                        if(!spot.MudPotential[pixel]){spot.MudPotential[pixel]=true;spot.OriginalPixels++;}
                        changed=true;
                    }
                }
            }
            if(changed)spot.MudRevision++;
            return changed;
        }
        private bool Clear(Spot spot)
        {
            spot.Cleared=true;ClearedCount++;Progress();
            if(ClearedCount==Spots.Count)Finish(false);return true;
        }
        public void Tick(double delta)
        {
            if(Settled||double.IsNaN(delta)||double.IsInfinity(delta)||delta<0)return;Elapsed+=delta;
            if(Config.TimeLimitSeconds>0&&Elapsed>=Config.TimeLimitSeconds){Elapsed=Config.TimeLimitSeconds;Finish(true);return;}
            if(Config.HintEnabled&&Elapsed-_lastProgress>=Config.HintAfterSeconds&&HintKey==null)HintKey=Spots.First(s=>!s.Cleared).Type.HintKey;
        }
        private void Finish(bool timeout)
        {
            if(Settled)return;Settled=true;Result=new PathCleaningResult{ElapsedMs=(int)Math.Min(int.MaxValue,Math.Round(Elapsed*1000)),Outcome=timeout?"timeout":"success",RewardId=timeout?Config.TimeoutRewardId:Elapsed<=Config.FastSeconds?Config.FastRewardId:Elapsed<=Config.MediumSeconds?Config.MediumRewardId:Config.SlowRewardId};
        }
    }
}
