using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.UI;

namespace PartAdjustment
{
    [ES3NonSerializable,DefaultExecutionOrder(10003)]
    public sealed class ToolGuide : MonoBehaviour
    {
        private Text label,useText;
        private Text seatPrompt;
        private string lastSeatPrompt;
        private Transform native;
        private bool nativeWasActive,ownsNative;
        private string lastUse,beforeUse,lastCursor;
        private Color beforeColor;
        private VerticalWrapMode beforeOverflow;
        private float nextFind;
        private PlayMakerFSM cursor;
        private void LateUpdate()
        {
            UltimateAdjustmentHighlight.Update();
            bool show=UltimateTool.Active&&AdjustmentRunner.Gameplay&&AdjustmentRunner.PlayerCamera!=null;
            if(!show){Hide();return;}
            if(label==null && !Create())return;
            if(!ownsNative){nativeWasActive=native.gameObject.activeSelf;ownsNative=true;}
            native.gameObject.SetActive(false); label.gameObject.SetActive(true);
            string use=Controls.Keys(Controls.Use),mode=Controls.Keys(Controls.Secondary);
            string put=Plugin.PutAwayWithKey.Value?Controls.Label(Plugin.PutAwayKey.Value)+" - put tool away":"";
            string text=(UltimateTool.Screwdriver?"SCREWDRIVER\nMOVE / ROTATE":"WRENCH\nATTACH / DETACH")+"\n"
                +mode+" - switch to "+(UltimateTool.Screwdriver?"wrench":"screwdriver")+"\n"+put;
            var session=AdjustmentRunner.Session;
            if(UltimateTool.Editing)
            {
                if(ToolRunner.Active!=null)
                {
                    var suspension=ToolRunner.Active;
                    text+="\n\nSUSPENSION\n"+SuspensionControls.Hint
                        +"\nWidth x"+suspension.Width.ToString("0.00")
                        +"   Height "+(suspension.RideHeight*100f).ToString("+0;-0;0")+" cm"
                        +"   Balance "+(suspension.Balance*100f).ToString("+0;-0;0")+" cm";
                }
                else
                {
                    bool rotate=session!=null?session.Rotating:NativeRotating();
                    text+="\n\n"+(rotate?"ROTATION MODE":"POSITION MODE")
                        +"\n"+Controls.Keys(Controls.Mode)+" - change move / rotate\n"
                        +Controls.Keys(Controls.Left)+" / "+Controls.Keys(Controls.Right)+" - left / right\n"
                        +Controls.Keys(Controls.Up)+" / "+Controls.Keys(Controls.Down)+" - "+(rotate?"pitch":"up / down")+"\n"
                        +Controls.Keys(Controls.Forward)+" / "+Controls.Keys(Controls.Backward)+" - "+(rotate?"roll":"forward / back")+"\n"
                        +Controls.Keys(Controls.Reset)+" - reset\n"+Controls.Label(Plugin.ModifierKey.Value)+" - faster";
                }
            }
            if(label.text!=text)label.text=text;
            string context=Context(use);
            bool drivePrompt=UltimateTool.Driving || (UltimateTool.DriveOffered && !UltimateTool.Holding);
            if(context!=null && !drivePrompt)SetUse(context);else RestoreUse();
            if(UltimateTool.DriveOffered && !UltimateTool.Driving && !UltimateTool.Screwdriver && UltimateTool.HeldId=="seat"
                && UltimateTool.Compatible(UltimateTool.TargetSlot,UltimateTool.Held)
                && UltimateTool.TargetSlot.name=="hinge_seat_driver")
            {
                var nameObject=FsmVariables.GlobalVariables.GetFsmGameObject("UI_ItemName")?.Value;
                seatPrompt=nameObject==null?null:nameObject.GetComponent<Text>();
                if(seatPrompt!=null){lastSeatPrompt="Mount driver seat ("+use+")";seatPrompt.text=lastSeatPrompt;}
            }
            else RestoreSeatPrompt();
            if(cursor==null)cursor=Resources.FindObjectsOfTypeAll<PlayMakerFSM>().FirstOrDefault(f=>f.FsmName=="MouseIcon"&&f.name=="Canvas"&&f.gameObject.scene.IsValid());
            string evt=drivePrompt?null:context==null?"Cursor_Point":UltimateTool.Screwdriver?"Cursor_Adjust":"Cursor_Wrench";
            if(cursor!=null&&evt!=null){cursor.SendEvent(evt);lastCursor=evt;}
        }
        private static bool NativeRotating()
        {
            var hinge=UltimateTool.Adjust?.FsmVariables.GetFsmGameObject("Hinge")?.Value;
            var f=AdjustmentRunner.Find(hinge,"AdjustHinge");return f!=null&&f.ActiveStateName=="rotate";
        }
        private static string Context(string use)
        {
            if(UltimateTool.Editing)return use+" - finish adjustment"+(UltimateTool.Holding?"\nHeld in place":"");
            if(UltimateTool.Holding)
            {
                string name=UltimateTool.Label(UltimateTool.Held);
                if(UltimateTool.Screwdriver)return name+" - held in place\n"+use+" - move / rotate held part";
                if(UltimateTool.Compatible(UltimateTool.TargetSlot,UltimateTool.Held))return name+" - held in place\n"+use+" - attach to "+UltimateTool.Label(UltimateTool.TargetSlot.gameObject);
                if(UltimateTool.FreeSurface(out var parent,out var vehicle))return name+" - held in place\n"+use+" - attach here";
                return name+" - held in place\nAim at a compatible attachment point";
            }
            var target=UltimateTool.Target;if(target==null)return null;
            var suspension=target.GetComponent<SuspensionPickTarget>();
            if(UltimateTool.Screwdriver && suspension!=null)return suspension.Adjustment.KitFitted?use+" - adjust suspension":null;
            if(UltimateTool.Screwdriver && UltimateTool.TargetHinge!=null)return UltimateTool.Label(UltimateTool.TargetHinge.gameObject)+"\n"+use+" - move / rotate";
            if(target.CompareTag("vehPart")&&AdjustmentRunner.Find(target,"de_Attach")!=null)
                return UltimateTool.Label(target)+"\n"+use+(UltimateTool.Screwdriver?" - move / rotate":" - detach and hold");
            return null;
        }
        private bool Create()
        {
            if(Time.unscaledTime<nextFind)return false;nextFind=Time.unscaledTime+1f;
            native=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t.name=="AdjustUI"&&t.gameObject.scene.IsValid());
            var source=native==null?null:native.GetComponent<Text>();var canvas=native==null?null:native.GetComponentInParent<Canvas>(true);
            if(source==null||canvas==null)return false;
            var go=new GameObject("UltimateTools.NativeGuide",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            go.transform.SetParent(canvas.rootCanvas.transform,false);go.AddComponent<UltimateTransient>();
            label=go.GetComponent<Text>();label.font=source.font;label.fontSize=source.fontSize;label.fontStyle=source.fontStyle;
            label.color=Color.white;label.alignment=TextAnchor.UpperLeft;label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;label.raycastTarget=false;
            var original=source.GetComponent<Outline>();var outline=go.AddComponent<Outline>();outline.effectColor=original==null?new Color(0,0,0,.8f):original.effectColor;outline.effectDistance=original==null?new Vector2(1,-1):original.effectDistance;
            var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(24,-26);rect.sizeDelta=new Vector2(900,420);
            return true;
        }
        private void SetUse(string text)
        {
            if(useText==null){var go=FsmVariables.GlobalVariables.GetFsmGameObject("UI_ItemUse")?.Value;useText=go==null?null:go.GetComponent<Text>();if(useText!=null){beforeUse=useText.text;beforeColor=useText.color;beforeOverflow=useText.verticalOverflow;}}
            if(useText==null)return;
            useText.color=Color.white;useText.verticalOverflow=VerticalWrapMode.Overflow;useText.text=text;lastUse=text;
        }
        private void RestoreUse(){if(useText!=null){if(useText.text==lastUse)useText.text=beforeUse??"";useText.color=beforeColor;useText.verticalOverflow=beforeOverflow;}useText=null;lastUse=null;beforeUse=null;}
        private void RestoreSeatPrompt(){if(seatPrompt!=null && seatPrompt.text==lastSeatPrompt)seatPrompt.text="";seatPrompt=null;lastSeatPrompt=null;}
        private void Hide(){if(label!=null)label.gameObject.SetActive(false);if(ownsNative&&native!=null){native.gameObject.SetActive(nativeWasActive && UltimateTool.BoxOut(AdjustmentRunner.Find(AdjustmentRunner.PlayerCamera==null?null:AdjustmentRunner.PlayerCamera.gameObject,"UseAdjustToolBox")));}ownsNative=false;RestoreUse();RestoreSeatPrompt();if(cursor!=null&&lastCursor!=null)cursor.SendEvent("Cursor_Point");lastCursor=null;}
        private void OnDestroy(){Hide();UltimateAdjustmentHighlight.Clear();if(label!=null)Destroy(label.gameObject);}
    }
}
