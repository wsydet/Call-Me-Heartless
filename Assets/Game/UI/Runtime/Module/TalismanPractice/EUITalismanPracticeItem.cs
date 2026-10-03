using System;
using Game.TalismanPractice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI
{
    public partial class EUITalismanPracticeItem
    {
        private TalismanPracticeGame _game;
        private bool _paused;
        private int _revision=-1;
        private readonly Button[,] _partButtons=new Button[3,3];
        private TalismanPatternGraphic _ink, _reference, _left, _right;
        private Image[][] _marks;
        private TalismanBreathInput _breathInput;
        public void Configure(TalismanPracticeGame game,Action start,Action next,Action<int> difference,
            Action clear,Action submit,Action<bool> blow,Action<int,int> select)
        {
            _game=game;
            StartButton.onClick.AddListener(()=>start()); NextButton.onClick.AddListener(()=>next());
            ClearButton.onClick.AddListener(()=>clear()); SubmitButton.onClick.AddListener(()=>submit());
            _breathInput=BlowButton.GetComponent<TalismanBreathInput>();
            if(!_breathInput) throw new InvalidOperationException("吹气输入组件缺失。");
            _breathInput.Configure(blow);
            _ink=DrawRoot.Find("Ink").GetComponent<TalismanPatternGraphic>();
            for(int layer=0;layer<3;layer++) for(int variant=0;variant<3;variant++)
            {
                int l=layer,v=variant;
                var button=ShopRoot.Find("PartsRoot/Part"+l+"_"+v).GetComponent<Button>();
                _partButtons[l,v]=button;
                button.onClick.AddListener(()=>{if(!_paused)select(l,v);});
            }
            _reference=ShopRoot.Find("ReferencePaper/Symbol").GetComponent<TalismanPatternGraphic>();
            _left=DifferenceRoot.Find("LeftPaper/Symbol").GetComponent<TalismanPatternGraphic>();
            _right=DifferenceRoot.Find("RightPaper/Symbol").GetComponent<TalismanPatternGraphic>();
            foreach(var target in DifferenceRoot.GetComponentsInChildren<TalismanDifferenceTarget>(true))
                target.Configure(i=>{if(!_paused) difference(i);});
            _marks=new Image[2][];
            for(int side=0;side<2;side++)
            {
                var paper=DifferenceRoot.Find(side==0?"LeftPaper":"RightPaper");
                _marks[side]=new[]{paper.Find("Difference0").GetComponent<Image>(),paper.Find("Difference1").GetComponent<Image>()};
            }
            Refresh(game,false);
        }
        public void Refresh(TalismanPracticeGame game,bool paused)
        {
            _game=game; _paused=paused;
            var bounds=(RectTransform)PracticePanel.parent;
            float scale=Mathf.Min(1,bounds.rect.width*.92f/1240f,bounds.rect.height*.61f/800f);
            PracticePanel.localScale=Vector3.one*Mathf.Max(.1f,scale);
            bool playing=game.Phase==PracticePhase.Playing && !paused;
            bool review=game.Phase==PracticePhase.Review;
            for(int layer=0;layer<3;layer++) for(int variant=0;variant<3;variant++)
            {
                var button=_partButtons[layer,variant];button.interactable=playing && game.Stage==PracticeStage.Orders;
                button.GetComponent<Image>().color=game.SelectedPart(layer)==variant ? new Color(.23f,.62f,.46f) : new Color(.17f,.24f,.27f);
            }
            DifferenceRoot.gameObject.SetActive(game.Stage==PracticeStage.Differences);
            AnswerRoot.gameObject.SetActive(false);
            ShopRoot.gameObject.SetActive(game.Stage==PracticeStage.Orders);
            FeatherRoot.gameObject.SetActive(game.Stage==PracticeStage.Feather);
            StartButton.gameObject.SetActive(false);
            NextButton.gameObject.SetActive(false);
            StartButton.interactable=NextButton.interactable=!paused;
            ClearButton.interactable=SubmitButton.interactable=playing && game.Stage==PracticeStage.Orders;
            BlowButton.interactable=playing && game.Stage==PracticeStage.Feather;
            _breathInput.SetAllowed(BlowButton.interactable);
            StageText.text=game.Stage==PracticeStage.Differences ? "识符找不同"
                :game.Stage==PracticeStage.Orders ? "画符接单" : $"静心吹羽毛 · 第 {game.Config.difficultyLevel} 关 · {game.Config.difficultyName}";
            TimerText.text=paused?"已暂停":game.Phase==PracticePhase.Playing ? "剩余 "+Mathf.CeilToInt(game.Remaining)+" 秒"
                : Mathf.CeilToInt(game.TransitionRemaining)+" 秒后"+(review?"继续":"开始");
            HintText.text=paused?"已暂停，返回后继续。":game.Hint;
            TimerText.gameObject.SetActive(!game.Config.tutorial && playing);
            if(!paused && !review)
                HintText.text = !game.Config.tutorial && game.Stage==PracticeStage.Orders ? game.Hint
                    : game.Elapsed < game.Config.tutorialHintDelay ? "" : game.Stage==PracticeStage.Feather && game.Config.frameMoveAmplitudeY > 0
                    ? "框也随风动起来了。看着它的方向，缓缓吹气跟上；离开框也不会丢掉已累计的时间。"
                    : !game.Config.tutorial ? "" : game.Stage==PracticeStage.Differences
                    ? (game.Result.differencesFound == 0 ? "仔细看看，两张符纸有两处笔画不同，点出来试试。" : "找到一处了，再对照另一处笔画。")
                    : game.Stage==PracticeStage.Orders
                    ? (game.SelectedPart(0)<0 ? "先看看客人要的符样，从右边选一个相同的底纹。" : game.SelectedPart(1)<0 ? "再给它搭上对应的中层图案。" : game.SelectedPart(2)<0 ? "最后补上细节，三层配齐就可以交给客人。" : game.Hint)
                    : (game.HoldTime<=0 ? "按住吹气按钮或空格，气流会慢慢变强；松开便停下，羽毛会缓缓飘落。" : "稳住呼吸，快到框上沿就松开，落低了再按住。飘出去也没关系。" );
            PatienceFill.transform.parent.Find("PatienceTrack").gameObject.SetActive(!game.Config.tutorial);
            PatienceFill.gameObject.SetActive(!game.Config.tutorial);
            ProgressText.text=game.Stage==PracticeStage.Differences ? string.Format("{0}   ·   第 {1}/3 张   ·   已找到 {2}/6 处",TalismanPatterns.Name(game.Round),game.Round+1,game.Result.differencesFound)
                :game.Stage==PracticeStage.Orders ? string.Format("已完成 {0}/{1} 单",game.Result.ordersCompleted,game.Config.customerCount)
                :string.Format("框内累计 {0:F1} / {1:F0} 秒   ·   {2}",game.HoldTime,game.Config.holdSeconds,game.InFrame?"保持住":"按住吹气");
            NextButton.GetComponentInChildren<TMP_Text>(true).text="返回剧情";
            OrderText.text=game.Customer>=game.Config.customerCount ? "今日接待结束" : "顾客 "+(game.Customer+1)+"："+TalismanPatterns.Name(game.Pattern);
            QueueText.text="待接待："+Mathf.Max(0,game.Config.customerCount-game.Customer)+" 位";
            CustomerPortrait.color=Color.Lerp(new Color(.32f,.45f,.55f),new Color(.63f,.41f,.4f),game.Pattern/2f);
            Fill(PatienceFill,240,game.Patience/game.Config.patienceSeconds);
            Fill(HoldFill,400,game.HoldTime/game.Config.holdSeconds);
            var area=(RectTransform)Feather.parent; float height=area.rect.height;
            Feather.anchoredPosition=new Vector2((game.FeatherX-.5f)*area.rect.width,(game.FeatherY-.5f)*height);
            Feather.localRotation=Quaternion.Euler(0,0,game.FeatherTilt);
            Feather.sizeDelta=new Vector2(2*game.Config.featherHalfWidth*area.rect.width,2*game.Config.featherHalfHeight*height);
            TargetFrame.anchoredPosition=new Vector2((game.FrameX-.5f)*area.rect.width,((game.FrameTop+game.FrameBottom)*.5f-.5f)*height);
            TargetFrame.sizeDelta=new Vector2(game.Config.frameWidth*area.rect.width,(game.FrameTop-game.FrameBottom)*height);
            foreach(string edge in new[]{"Top","Bottom"})
            {var r=(RectTransform)TargetFrame.Find(edge);r.sizeDelta=new Vector2(TargetFrame.sizeDelta.x,3);r.anchoredPosition=new Vector2(0,(edge=="Top"?1:-1)*TargetFrame.sizeDelta.y*.5f);}
            foreach(string edge in new[]{"Left","Right"})
            {var r=(RectTransform)TargetFrame.Find(edge);r.sizeDelta=new Vector2(3,TargetFrame.sizeDelta.y);r.anchoredPosition=new Vector2((edge=="Right"?1:-1)*TargetFrame.sizeDelta.x*.5f,0);}
            if(_revision != game.Revision)
            {
                _revision=game.Revision;
                _left.Show(game.Round,false); _right.Show(game.Round,true);
                _reference.Show(game.Pattern,false); _ink.Show(game.Pattern,false,game.SelectedParts);
                for(int s=0;s<2;s++) for(int i=0;i<2;i++) _marks[s][i].color=game.Found(i)?new Color(.2f,.75f,.55f,.32f):Color.clear;
            }
        }
        private static void Fill(Image image,float width,float fraction)
        {
            var rect=image.rectTransform; float w=width*Mathf.Clamp01(fraction);
            rect.sizeDelta=new Vector2(w,rect.sizeDelta.y);
            float origin=image.name=="PatienceFill"?-223:0;
            rect.anchoredPosition=new Vector2(origin-(width-w)*.5f,rect.anchoredPosition.y);
        }
        public override void OnDispose()
        {
            _breathInput?.Clear();
            _game=null;
            foreach(var button in _partButtons) if(button)button.onClick.RemoveAllListeners();
            foreach(var target in DifferenceRoot.GetComponentsInChildren<TalismanDifferenceTarget>(true)) target.Configure(null);
            foreach(var button in new[]{StartButton,NextButton,ClearButton,SubmitButton,BlowButton}) button.onClick.RemoveAllListeners();
            base.OnDispose();
        }
    }
}
