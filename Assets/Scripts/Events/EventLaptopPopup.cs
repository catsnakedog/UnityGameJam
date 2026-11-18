using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UnityGameJam.Events
{
    public sealed class EventLaptopPopup : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text number,title,body,buttonLabel,resultLabel,hint;
        [SerializeField] Button confirm,yesButton,noButton;
        [SerializeField] ScrollRect scroll;
        [SerializeField,Min(1)] float charactersPerSecond=30;
        [SerializeField,Min(0.5f)] float resultDisplaySeconds=3;
        readonly List<int> visibleSteps=new List<int>();
        float elapsed,resultElapsed;
        int lastInputFrame=-1;
        GameObject previousSelection;
        public bool IsOpen { get; private set; }
        public bool ShowingResult { get; private set; }
        public bool IsTyping => IsOpen && !ShowingResult && body.maxVisibleCharacters<body.textInfo.characterCount;
        public bool CanChoose => IsOpen && !IsTyping && !ShowingResult;
        public int VisibleCharacters => body.maxVisibleCharacters;
        public event Action Closed;
        public event Action<bool> ChoiceRequested;
        void Awake()
        {
            panel.SetActive(false);
            confirm.onClick.AddListener(Confirm); yesButton.onClick.AddListener(Yes); noButton.onClick.AddListener(No);
        }
        void OnDestroy()
        {
            if(confirm) confirm.onClick.RemoveListener(Confirm);
            if(yesButton) yesButton.onClick.RemoveListener(Yes);
            if(noButton) noButton.onClick.RemoveListener(No);
        }
        void OnDisable() { Close(); }
        void Yes() => RequestChoice(true);
        void No() => RequestChoice(false);
        public void Show(EventDefinition entry)
        {
            if(IsOpen) throw new InvalidOperationException("Event popup is already open.");
            number.text="EVENT / "+entry.Id;
            title.text=entry.Title; body.richText=false; body.text=entry.Content;
            body.maxVisibleCharacters=int.MaxValue;
            panel.SetActive(true); IsOpen=true; ShowingResult=false; resultLabel.text="";
            Canvas.ForceUpdateCanvases(); body.ForceMeshUpdate();
            var boundaries=StringInfo.ParseCombiningCharacters(entry.Content);
            visibleSteps.Clear(); int visible=0;
            for(int i=0;i<boundaries.Length;i++)
            {
                int end=i+1<boundaries.Length?boundaries[i+1]:entry.Content.Length;
                while(visible<body.textInfo.characterCount && body.textInfo.characterInfo[visible].index<end) visible++;
                visibleSteps.Add(visible);
            }
            elapsed=0; body.maxVisibleCharacters=0; scroll.verticalNormalizedPosition=1;
            lastInputFrame=Time.frameCount;
            SetButtons();
            if(EventSystem.current) { previousSelection=EventSystem.current.currentSelectedGameObject; EventSystem.current.SetSelectedGameObject(null); }
        }
        void SetButtons()
        {
            confirm.gameObject.SetActive(IsTyping || ShowingResult);
            buttonLabel.text=ShowingResult?"계속":"전체 보기";
            hint.text=ShowingResult?"결과 확인 후 계속됩니다":IsTyping?"ENTER 또는 SPACE · 전체 보기":"Y / N · 선택";
            yesButton.gameObject.SetActive(CanChoose); noButton.gameObject.SetActive(CanChoose);
        }
        void Update()
        {
            if(!IsOpen) return;
            if(ShowingResult)
            {
                resultElapsed+=Time.unscaledDeltaTime;
                if(resultElapsed>=Mathf.Max(0.5f,resultDisplaySeconds)) { Close(); return; }
            }
            else if(IsTyping)
            {
                elapsed+=Time.unscaledDeltaTime*Mathf.Max(1,charactersPerSecond);
                int count=Mathf.Min(visibleSteps.Count,Mathf.FloorToInt(elapsed));
                body.maxVisibleCharacters=count==0?0:visibleSteps[count-1];
                if(!IsTyping) SetButtons();
            }
            var k=Keyboard.current;
            if(k==null) return;
            if(k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Confirm();
            else if(k.yKey.wasPressedThisFrame) RequestChoice(true);
            else if(k.nKey.wasPressedThisFrame) RequestChoice(false);
        }
        public void Confirm()
        {
            if(!IsOpen || Time.frameCount==lastInputFrame) return;
            lastInputFrame=Time.frameCount;
            if(IsTyping) { body.maxVisibleCharacters=body.textInfo.characterCount; SetButtons(); }
            else if(ShowingResult) Close();
        }
        public void RequestChoice(bool yes)
        {
            if(!CanChoose || Time.frameCount==lastInputFrame) return;
            lastInputFrame=Time.frameCount; ChoiceRequested?.Invoke(yes);
        }
        public void ShowResult(string result)
        {
            if(!IsOpen) return;
            body.maxVisibleCharacters=body.textInfo.characterCount;
            resultLabel.text=result; ShowingResult=true; resultElapsed=0; lastInputFrame=Time.frameCount; SetButtons();
        }
        public void ShowUnavailable(string message) => ShowResult(message);
        public void ShowChoiceError(string message) { resultLabel.text=message; SetButtons(); }
        public void Close()
        {
            if(!IsOpen) return;
            IsOpen=false; ShowingResult=false; panel.SetActive(false);
            body.text=title.text=number.text=resultLabel.text=string.Empty; visibleSteps.Clear();
            if(EventSystem.current && previousSelection && previousSelection.activeInHierarchy) EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection=null; Closed?.Invoke();
        }
    }
}
