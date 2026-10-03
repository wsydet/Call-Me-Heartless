using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using Game.Narrative;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.TalismanPractice.Editor
{
    public static class TalismanPracticeSetup
    {
        public const string PrefabPath="Assets/GameResource/Resources/UI/Module/TalismanPractice/Prefabs/EUITalismanPracticeItem.prefab";
        public const string StepPath="Assets/GameResource/Authoring/Narrative/Scripts/TalismanPractice/TalismanPracticeStep.asset";
        public const string StoryPath="Assets/GameResource/Resources/Config/Narrative/CallMeHeartless/Story_CallMeHeartless.asset";
        public const string ChapterPath="Assets/GameResource/Resources/Config/Narrative/CallMeHeartless/Chapters/CH03_Chapter02/Chapter.asset";
        public const string NodePath="Assets/GameResource/Resources/Config/Narrative/CallMeHeartless/Chapters/CH03_Chapter02/Dialogue/CH03_Chapter02_D_Action.asset";
        private static readonly Color Paper=new Color(.92f,.83f,.64f), Ink=new Color(.64f,.2f,.15f), Accent=new Color(.45f,.8f,.69f);
        private static TMP_FontAsset _font;
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform)); var r=(RectTransform)go.transform;
            r.SetParent(parent,false); r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
            r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); return r;
        }
        private static Image Box(Transform parent,string name,float x,float y,float w,float h,Color color,bool hit=false)
        { var r=Rect(parent,name,x,y,w,h); var image=r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=hit; return image; }
        private static TMP_Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size=24)
        {
            var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
            t.font=_font; t.text=text; t.fontSize=size; t.color=new Color(.91f,.93f,.9f); t.alignment=TextAlignmentOptions.Center;
            t.textWrappingMode=TextWrappingModes.Normal; t.raycastTarget=false; return t;
        }
        private static Button Button(Transform parent,string name,string text,float x,float y,float width=180)
        {
            var im=Box(parent,name,x,y,width,48,new Color(.15f,.35f,.31f),true);
            var button=im.gameObject.AddComponent<Button>(); button.targetGraphic=im;
            Label(im.transform,"Label",text,0,0,width-12,42,23); return button;
        }
        private static Type UIType(string name)
            => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Game.UI."+name)).FirstOrDefault(t=>t!=null)
                ?? throw new InvalidOperationException("先编译 UI 组件："+name);
        private static Component Symbol(Transform parent,string name,int pattern,bool wrong,float x,float y,float w,float h,bool draw=false)
        {
            var r=Rect(parent,name,x,y,w,h); var component=r.gameObject.AddComponent(UIType("TalismanPatternGraphic"));
            var so=new SerializedObject(component);
            so.FindProperty("pattern").intValue=pattern; so.FindProperty("incorrect").boolValue=wrong; so.FindProperty("assembled").boolValue=draw;
            so.ApplyModifiedPropertiesWithoutUndo();
            var graphic=(Graphic)component; graphic.color=Ink; graphic.raycastTarget=false; return component;
        }
        private static void DifferencePaper(Transform parent,string name,bool wrong,float x)
        {
            var paper=Box(parent,name,x,25,300,380,Paper,true);
            var miss=paper.gameObject.AddComponent(UIType("TalismanDifferenceTarget"));
            Symbol(paper.transform,"Symbol",0,wrong,0,0,300,380);
            for(int i=0;i<2;i++)
            {
                var center=TalismanPatterns.DifferenceCenter(0,i);
                var target=Box(paper.transform,"Difference"+i,(center.x-.5f)*300,(center.y-.5f)*380,210,96,new Color(0,0,0,0),true);
                var handler=target.gameObject.AddComponent(UIType("TalismanDifferenceTarget"));
                var so=new SerializedObject(handler); so.FindProperty("difference").intValue=i; so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        [MenuItem("Call Me Heartless/D 练习/创建或重新生成 UI")]
        public static void BuildUI()
        {
            var donor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Resources/UI/Module/GardenCare/Prefabs/EUIGardenCareItem.prefab");
            _font=donor ? donor.GetComponentInChildren<TMP_Text>(true)?.font : null;
            if(!_font) throw new InvalidOperationException("项目中文字体未找到。");
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            {
                var request=new EUICreationRequest { PrefabName="EUITalismanPracticeItem",ClassName="EUITalismanPracticeItem",
                    ClassPath="Module/TalismanPractice",Role=EUIBindingRole.Item,CodePathMode=EUIBinding.CodePathMode.Business,
                    GenerateCustomSettings=false };
                if(!EUICreationService.TryBuildPlan(request,out var plan,out var preflight)) throw new InvalidOperationException(preflight.Error);
                var created=EUICreationService.Create(request);
                if(!created.Success) throw new InvalidOperationException(created.Error);
            }
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if(!root.transform.Find("PracticePanel"))
                {
                    var rootRect=root.GetComponent<RectTransform>(); rootRect.anchorMin=Vector2.zero; rootRect.anchorMax=Vector2.one;
                    rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;
                    var blocker=root.GetComponent<Image>() ?? root.AddComponent<Image>(); blocker.color=Color.clear; blocker.raycastTarget=false;
                    var panel=Rect(root.transform,"PracticePanel",0,0,1200,760);
                    panel.anchorMin=panel.anchorMax=new Vector2(.5f,.595f);
                    Box(panel,"HeaderRule",0,258,1080,2,new Color(.26f,.39f,.39f));
                    Label(panel,"StageText","01  识符     /     02  画符接单     /     03  静心",0,322,1000,52,30);
                    Label(panel,"TimerText","准备练习",430,282,210,38,21);
                    Label(panel,"ProgressText","跟师父练习",0,-262,1080,44,23);
                    Label(panel,"HintText","找出符纸中的错笔。",-30,-310,1000,62,24);
                    Button(panel,"StartButton","开始练习",0,-350);
                    Button(panel,"NextButton","继续",0,-350);
                    var diff=Rect(panel,"DifferenceRoot",0,0,1080,490);
                    Label(diff,"LeftTitle","师父的符样",-260,240,350,35);
                    Label(diff,"RightTitle","你的习作",260,240,350,35);
                    DifferencePaper(diff,"LeftPaper",false,-260); DifferencePaper(diff,"RightPaper",true,260);
                    var answers=Rect(panel,"AnswerRoot",0,0,1080,490);
                    Label(answers,"Title","记住这三种符样，顾客会用到它们",0,222,1000,40);
                    for(int i=0;i<3;i++)
                    {
                        var paper=Box(answers,"Answer"+i,(i-1)*290,15,220,290,Paper);
                        Symbol(paper.transform,"Symbol",i,false,0,0,220,290);
                        Label(answers,"Name"+i,TalismanPatterns.Name(i),(i-1)*290,-165,230,36);
                    }
                    var shop=Rect(panel,"ShopRoot",0,0,1080,490);
                    var portrait=Box(shop,"CustomerPortrait",-405,151,76,82,new Color(.34f,.48f,.55f));
                    Label(portrait.transform,"Face","客",0,0,64,70,35);
                    Label(shop,"OrderText","顾客 1 / 5",-223,174,250,38);
                    Box(shop,"PatienceTrack",-223,129,240,12,new Color(.22f,.26f,.27f));
                    Box(shop,"PatienceFill",-223,129,240,12,Accent);
                    var reference=Box(shop,"ReferencePaper",-310,-62,210,260,Paper);
                    Symbol(reference.transform,"Symbol",0,false,0,0,210,260);
                    var draw=Box(shop,"DrawRoot",190,0,340,420,Paper);
                    Symbol(draw.transform,"Ink",0,false,0,0,340,420,true);
                    Label(shop,"PaperTitle","在这里描画",190,231,340,34,23);
                    Label(shop,"QueueText","待接待：5 位",-310,-213,350,32,20);
                    Button(shop,"ClearButton","换张符纸",463,72,160); Button(shop,"SubmitButton","交给顾客",463,0,160);
                    var feather=Rect(panel,"FeatherRoot",0,0,1080,490);
                    var area=Box(feather,"FlightArea",0,10,400,420,new Color(.09f,.15f,.19f),true);
                    var frame=Box(area.transform,"TargetFrame",0,12.6f,320,117.6f,new Color(.3f,.64f,.51f,.15f));
                    Box(frame.transform,"Top",0,58.8f,320,3,Accent); Box(frame.transform,"Bottom",0,-58.8f,320,3,Accent);
                    Box(frame.transform,"Left",-160,0,3,117.6f,Accent); Box(frame.transform,"Right",160,0,3,117.6f,Accent);
                    var f=Box(area.transform,"Feather",0,-126,24,33.6f,new Color(.87f,.92f,.94f));
                    Label(f.transform,"Mark","羽",0,0,24,33,20).color=new Color(.2f,.35f,.4f);
                    Label(feather,"Direction","点击吹气\n松手自然下落",-365,25,240,90);
                    Button(feather,"BlowButton","吹气",366,0);
                    Box(feather,"HoldTrack",0,-219,400,12,new Color(.22f,.26f,.27f));
                    Box(feather,"HoldFill",0,-219,400,12,Accent);
                    answers.gameObject.SetActive(false); shop.gameObject.SetActive(false); feather.gameObject.SetActive(false);
                    panel.Find("NextButton").gameObject.SetActive(false);
                    var entries=new List<EUIBindingEntrySnapshot>();
                    Action<string,string,string> bind=(name,path,type)=>entries.Add(new EUIBindingEntrySnapshot
                    {Name=name,GameObjectPath="PracticePanel/"+path,WidgetType=type=="TMPro.TMP_Text"?EUIBinding.WidgetTypes.Text:type=="UnityEngine.UI.Button"?EUIBinding.WidgetTypes.Button:type=="UnityEngine.UI.Image"?EUIBinding.WidgetTypes.Image:EUIBinding.WidgetTypes.Extension,ClassName=type});
                    foreach(var name in new[]{"StageText","TimerText","ProgressText","HintText"}) bind(name,name,"TMPro.TMP_Text");
                    foreach(var name in new[]{"StartButton","NextButton"}) bind(name,name,"UnityEngine.UI.Button");
                    foreach(var name in new[]{"DifferenceRoot","AnswerRoot","ShopRoot","FeatherRoot"}) bind(name,name,"UnityEngine.RectTransform");
                    bind("PracticePanel","","UnityEngine.RectTransform");
                    bind("DrawRoot","ShopRoot/DrawRoot","UnityEngine.RectTransform");
                    bind("ClearButton","ShopRoot/ClearButton","UnityEngine.UI.Button"); bind("SubmitButton","ShopRoot/SubmitButton","UnityEngine.UI.Button");
                    bind("OrderText","ShopRoot/OrderText","TMPro.TMP_Text"); bind("QueueText","ShopRoot/QueueText","TMPro.TMP_Text");
                    bind("CustomerPortrait","ShopRoot/CustomerPortrait","UnityEngine.UI.Image"); bind("PatienceFill","ShopRoot/PatienceFill","UnityEngine.UI.Image");
                    bind("BlowButton","FeatherRoot/BlowButton","UnityEngine.UI.Button"); bind("HoldFill","FeatherRoot/HoldFill","UnityEngine.UI.Image");
                    bind("TargetFrame","FeatherRoot/FlightArea/TargetFrame","UnityEngine.RectTransform"); bind("Feather","FeatherRoot/FlightArea/Feather","UnityEngine.RectTransform");
                    EUIBindingEditorUtility.SetBindings(root.GetComponent<EUIBinding>(),entries);
                    PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                }
                var currentBinding=root.GetComponent<EUIBinding>();
                var snapshot=EUIBindingEditorUtility.GetBindingSnapshot(currentBinding);
                foreach(var entry in snapshot.Entries)
                {
                    if(entry.ClassName=="TMPro.TMP_Text")entry.WidgetType=EUIBinding.WidgetTypes.Text;
                    else if(entry.ClassName=="UnityEngine.UI.Button")entry.WidgetType=EUIBinding.WidgetTypes.Button;
                    else if(entry.ClassName=="UnityEngine.UI.Image")entry.WidgetType=EUIBinding.WidgetTypes.Image;
                }
                EUIBindingEditorUtility.SetBindings(currentBinding,snapshot.Entries);
                BuildParts(root);
                foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
                    if(!graphic.GetComponent<CanvasRenderer>())graphic.gameObject.AddComponent<CanvasRenderer>();
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var binding=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<EUIBinding>();
            if(!EUIBindingCodeGenUtility.TryRegenerateCode(binding,out var error)) throw new InvalidOperationException(error);
            Directory.CreateDirectory(".utmp/vn-custom-node/20261002-d-talisman-practice-01/results");
            File.WriteAllText(".utmp/vn-custom-node/20261002-d-talisman-practice-01/results/binding.json",
                JsonUtility.ToJson(new BindingReport{prefab=PrefabPath, entries=EUIBindingEditorUtility.GetBindingSnapshot(binding).Entries.Select(e=>e.Name+" | "+e.GameObjectPath+" | "+e.WidgetType+" | "+e.ClassName).ToArray()},true));
            AssetDatabase.SaveAssets();
        }
        private static void BuildParts(GameObject root)
        {
            var shop=root.transform.Find("PracticePanel/ShopRoot");
            if(shop.Find("PartsRoot")) return;
            var preview=shop.Find("DrawRoot/Ink").GetComponent<Graphic>();
            var previewData=new SerializedObject(preview);previewData.FindProperty("assembled").boolValue=true;previewData.ApplyModifiedPropertiesWithoutUndo();preview.raycastTarget=false;
            foreach(string side in new[]{"LeftPaper","RightPaper"})for(int i=0;i<2;i++) {var target=(RectTransform)root.transform.Find("PracticePanel/DifferenceRoot/"+side+"/Difference"+i);var center=TalismanPatterns.DifferenceCenter(0,i);target.anchoredPosition=new Vector2((center.x-.5f)*300,(center.y-.5f)*380);}
            void Move(string path,float x,float y,float w,float h)
            { var r=(RectTransform)shop.Find(path);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h); }
            Move("CustomerPortrait",-474,167,64,72);
            Move("OrderText",-286,185,295,40);
            Move("PatienceTrack",-223,137,240,12);Move("PatienceFill",-223,137,240,12);
            Move("ReferencePaper",-413,-28,180,270);
            Move("ReferencePaper/Symbol",0,0,180,270);
            Move("DrawRoot",-156,-28,230,270);Move("DrawRoot/Ink",0,0,230,270);
            Move("PaperTitle",-156,121,245,32);shop.Find("PaperTitle").GetComponent<TMP_Text>().text="你的组合";
            Move("QueueText",-413,-181,200,30);
            Move("ClearButton",-365,-218,170,44);Move("SubmitButton",-145,-218,170,44);
            shop.Find("ClearButton/Label").GetComponent<TMP_Text>().text="清空组合";
            Label(shop,"ReferenceTitle","顾客需要",-413,121,180,32,21);
            var parts=Rect(shop,"PartsRoot",275,0,390,470);
            for(int layer=0;layer<3;layer++)
            {
                float y=156-layer*151;
                Label(parts,"Layer"+layer,TalismanPatterns.LayerName(layer),0,y+55,350,28,21);
                for(int variant=0;variant<3;variant++)
                {
                    var option=Button(parts,"Part"+layer+"_"+variant,TalismanPatterns.PartName(layer,variant),(variant-1)*117,y-9,105);
                    var r=(RectTransform)option.transform;r.sizeDelta=new Vector2(105,106);
                    var label=option.transform.Find("Label").GetComponent<TMP_Text>();label.fontSize=18;
                    label.rectTransform.anchoredPosition=new Vector2(0,-39);label.rectTransform.sizeDelta=new Vector2(101,28);
                    var glyph=Symbol(option.transform,"Symbol",0,false,0,5,81,76);
                    var so=new SerializedObject(glyph);so.FindProperty("layerOnly").intValue=layer;so.FindProperty("variant").intValue=variant;
                    so.FindProperty("thickness").floatValue=3;so.ApplyModifiedPropertiesWithoutUndo();
                    ((Graphic)glyph).color=Paper;
                }
            }
        }
        [Serializable] private sealed class BindingReport { public string prefab; public string[] entries; }
        [Serializable] private sealed class Commands { public List<NovelCommand> _commands; }
        [Serializable] private sealed class Variables { public List<NovelVariable> _variables; }
        [Serializable] private sealed class Globals { public List<NovelVariable> _globals; }
        [MenuItem("Call Me Heartless/D 练习/接入剧情")]
        public static void InstallNarrative()
        {
            var story=AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(StoryPath);
            var chapter=AssetDatabase.LoadAssetAtPath<NarrativeChapterSO>(ChapterPath);
            var node=AssetDatabase.LoadAssetAtPath<NarrativeDialogueSO>(NodePath);
            if(!story || !chapter || !node || node.Next == null ||
                (node.Next.name != "CH03_Chapter02_D_Return" && node.Next.name != "CH03_Chapter02_D_StudyChoice"))
                throw new InvalidOperationException("D 现有流程与确认方案不符。");
            var step=AssetDatabase.LoadAssetAtPath<TalismanPracticeStepSO>(StepPath);
            if(!step) { Directory.CreateDirectory(Path.GetDirectoryName(StepPath)); step=ScriptableObject.CreateInstance<TalismanPracticeStepSO>(); AssetDatabase.CreateAsset(step,StepPath); }
            InstallStudyChoice(chapter,node,step);
            var globals=story.Globals.ToList();
            foreach(string id in new[]{"ch2_gardenLearned","ch2_studyLearned_0","ch2_studyLearned_1","ch2_studyLearned_2"})
                if(!globals.Any(v=>v.Id==id))globals.Add(new NovelVariable(id,new NovelValue(false)));
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Globals{_globals=globals}),story);EditorUtility.SetDirty(story);
            if(!chapter.Variables.Any(v=>v.Id == "d_talismanOutcome"))
            {
                var variables=chapter.Variables.ToList(); variables.Add(new NovelVariable("d_talismanOutcome",new NovelValue("")));
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Variables{_variables=variables}),chapter); EditorUtility.SetDirty(chapter);
            }
            if(!story.CustomSteps.Any(s=>s && s.ScriptId == step.ScriptId))
            {
                var so=new SerializedObject(story); var list=so.FindProperty("_customSteps");
                int i=list.arraySize; list.InsertArrayElementAtIndex(i); list.GetArrayElementAtIndex(i).objectReferenceValue=step;
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(story);
            }
            AssetDatabase.SaveAssets();
        }

        private static void InstallStudyChoice(NarrativeChapterSO chapter, NarrativeDialogueSO action, TalismanPracticeStepSO step)
        {
            string folder=Path.GetDirectoryName(NodePath).Replace('\\','/');
            string choicePath=folder.Replace("/Dialogue","/Choices")+"/CH03_Chapter02_D_StudyChoice.asset";
            var choice=AssetDatabase.LoadAssetAtPath<NarrativeChoiceSO>(choicePath);
            if(choice && action.Next==choice) return;
            if(choice) throw new InvalidOperationException("学习选项资产已存在但接线不同，请检查现有剧情。");
            choice=ScriptableObject.CreateInstance<NarrativeChoiceSO>();
            AssetDatabase.CreateAsset(choice,choicePath);
            var added=new List<NarrativeNodeSO>{choice};
            var choiceSO=new SerializedObject(choice);
            choiceSO.FindProperty("_chapterId").stringValue=chapter.ChapterId;
            choiceSO.FindProperty("_flow").objectReferenceValue=action.Flow;
            choiceSO.FindProperty("_prompt").stringValue="今天想跟师父学习什么？";
            var options=choiceSO.FindProperty("_options"); options.arraySize=3;
            string[] names={"识符找不同","画符接单","静心吹羽毛"};
            for(int i=0;i<3;i++)
            {
                var node=ScriptableObject.CreateInstance<NarrativeDialogueSO>();
                AssetDatabase.CreateAsset(node,folder+"/CH03_Chapter02_D_Study"+i+".asset");
                var nodeSO=new SerializedObject(node);
                nodeSO.FindProperty("_chapterId").stringValue=chapter.ChapterId;
                nodeSO.FindProperty("_flow").objectReferenceValue=action.Flow;
                nodeSO.FindProperty("_next").objectReferenceValue=action.Next;
                nodeSO.ApplyModifiedPropertiesWithoutUndo();
                var command=new NovelCommand(i==0 ? "cmh_d_talisman_training_v1" : "cmh_d_talisman_training_"+i,
                    NovelCommandKind.CustomStep,customStepId:step.ScriptId,integerOperand:i);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands{_commands=new List<NovelCommand>{command}}),node);
                EditorUtility.SetDirty(node); added.Add(node);
                var option=options.GetArrayElementAtIndex(i);
                option.FindPropertyRelative("_optionId").stringValue="cmh_d_study_"+i;
                option.FindPropertyRelative("_text").stringValue=names[i];
                option.FindPropertyRelative("_textKey").stringValue="";
                option.FindPropertyRelative("_target").objectReferenceValue=node;
            }
            choiceSO.ApplyModifiedPropertiesWithoutUndo();
            var commands=action.Commands.Where(c=>c.Kind!=NovelCommandKind.CustomStep || c.CustomStepId!=step.ScriptId).ToList();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands{_commands=commands}),action);
            var actionSO=new SerializedObject(action);
            actionSO.FindProperty("_next").objectReferenceValue=choice;
            actionSO.FindProperty("_contentRevision").intValue++;
            actionSO.ApplyModifiedPropertiesWithoutUndo();
            var chapterSO=new SerializedObject(chapter);var nodes=chapterSO.FindProperty("_nodes");
            foreach(var node in added){int i=nodes.arraySize;nodes.InsertArrayElementAtIndex(i);nodes.GetArrayElementAtIndex(i).objectReferenceValue=node;}
            chapterSO.FindProperty("_storyRevision").intValue++;
            chapterSO.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
