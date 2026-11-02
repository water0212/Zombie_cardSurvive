#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;
using ZombieCardSurvive.UI.Morale;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.UI.Settlement.Editor
{
    public static class SettlementE2Setup
    {
        [MenuItem("Tools/QA/E2/Install presentation in current scene")]
        public static void Install()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Install in Edit mode.");
            var hud = Object.FindObjectOfType<GameplayHudRuntimePresenter>(true);
            var morale = Object.FindObjectOfType<MoraleHudPresenter>(true);
            var run = Object.FindObjectOfType<RunPhaseController>(true);
            if (hud == null || morale == null || run == null) throw new System.InvalidOperationException("Install E1 HUD first.");
            if (Object.FindObjectOfType<SettlementPresenter>(true) != null) return;
            var parent = hud.GetComponentInParent<Canvas>();
            if (parent == null) parent = Object.FindObjectOfType<Canvas>();
            var root = Rect("E2Settlement", parent.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Install E2 presentation");
            root.gameObject.AddComponent<Canvas>().overrideSorting = true;
            root.GetComponent<Canvas>().sortingOrder = 200;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var font = hud.GetResourceText("Food").font;
            var blocker = Panel("InputBlocker", root, new Color(0,0,0,.28f));
            var stage = Rect("StageTransition", root, new Vector2(.2f,.2f), new Vector2(.8f,.8f), Vector2.zero, Vector2.zero);
            stage.gameObject.AddComponent<Image>().color = new Color(.10f,.12f,.13f,.97f);
            var layers = new Image[3];
            for(int i=0;i<3;i++)
            {
                float half=.26f*(i+1)/3f;
                var layer = Rect("PyramidLayer"+i, stage, new Vector2(.5f-half,.73f-i*.15f),new Vector2(.5f+half,.87f-i*.15f),Vector2.zero,Vector2.zero);
                var image=layer.gameObject.AddComponent<PyramidLayerImage>(); image.Configure(i);layers[i]=image;
                var mark=Text("PermanentDamage",layer,font,30,new Vector2(0,0),Vector2.one);
                mark.alignment=TextAlignmentOptions.Center;
                mark.text="";
            }
            var stageBody=Text("StageEffects",stage,font,25,new Vector2(.04f,.05f),new Vector2(.96f,.37f));
            var summary=Text("LastStageSummary",root,font,19,new Vector2(.80f,.24f),new Vector2(.98f,.40f));
            var report=Panel("CollapseReport",root,new Color(.08f,.1f,.11f,.98f));
            var body=Text("Causes",report.transform,font,29,new Vector2(.15f,.20f),new Vector2(.85f,.87f));
            var restart=Button("Restart",report.transform,font,"重新開始",.32f);
            var leave=Button("Leave",report.transform,font,"離開本局",.56f);
            var collapse=root.gameObject.AddComponent<CollapseReportView>();
            collapse.Configure(report,body,restart,leave);
            var views=new List<ResourceValuePresenter>();
            foreach(var id in new[]{ValueId.Food,ValueId.Resource,ValueId.Combat,ValueId.Energy})
            {
                var number=hud.GetResourceText(id.ToString());
                if(number==null)continue;
                var delta=Text(id+"Delta",number.transform,font,19,new Vector2(.72f,.02f),new Vector2(1.18f,.45f));
                var view=number.gameObject.AddComponent<ResourceValuePresenter>();
                view.Configure(id,number,delta); views.Add(view);
            }
            var presenter=root.gameObject.AddComponent<SettlementPresenter>();
            presenter.Configure(run,hud,morale,views.ToArray(),blocker,stage.gameObject,stageBody,summary,layers,collapse);
            blocker.SetActive(false); stage.gameObject.SetActive(false); report.SetActive(false);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=lo;rect.offsetMax=hi;return rect;
        }
        private static GameObject Panel(string name,Transform parent,Color colour)
        { var rect=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);rect.gameObject.AddComponent<Image>().color=colour;return rect.gameObject; }
        private static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,float size,Vector2 min,Vector2 max)
        {
            var rect=Rect(name,parent,min,max,Vector2.zero,Vector2.zero);
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.raycastTarget=false;
            text.color=new Color32(242,234,214,255);text.text="";text.enableWordWrapping=true;return text;
        }
        private static Button Button(string name,Transform parent,TMP_FontAsset font,string label,float x)
        {
            var rect=Rect(name,parent,new Vector2(x,.06f),new Vector2(x+.15f,.14f),Vector2.zero,Vector2.zero);
            rect.gameObject.AddComponent<Image>().color=new Color(.22f,.27f,.28f);
            var button=rect.gameObject.AddComponent<Button>();var text=Text("Label",rect,font,24,Vector2.zero,Vector2.one);
            text.text=label;text.alignment=TextAlignmentOptions.Center;return button;
        }
        [MenuItem("Tools/QA/E2/Stage 0 to 2")]
        public static void TwoStages() { RequirePlay(); MoraleSystem.Reset(startingMorale:80); MoraleSystem.CommitChange("QA:TwoStages",-55); }
        [MenuItem("Tools/QA/E2/Collapse complete batch")]
        public static void Collapse()
        {
            RequirePlay(); var request=new MoraleBatchRequest("QA:RoundEnd");
            request.Add("FoodShortage",-100,0); request.Add("ZombieLeak",-10,-5); MoraleSystem.CommitBatch(request);
        }
        [MenuItem("Tools/QA/E2/Reduced motion")]
        public static void ReducedMotion(){RequirePlay();Object.FindObjectOfType<SettlementPresenter>().SetReducedMotion(true);}
        private static void RequirePlay(){if(!Application.isPlaying)throw new System.InvalidOperationException("QA requires Play mode.");}
    }
}
#endif
