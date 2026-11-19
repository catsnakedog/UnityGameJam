using GameJam.Audio;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

namespace UnityGameJam.Events
{
    public sealed class EventLaptopPopup : MonoBehaviour
    {
        enum Page { Hidden, Event, EventResult, Presentation }

        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text number,title,body,buttonLabel,resultLabel;
        [SerializeField] Button confirm,yesButton,noButton;
        [SerializeField] ScrollRect scroll;
        [SerializeField] GameObject presentationSheet;
        [SerializeField] TMP_Text reviewPlanning, reviewProgramming, reviewGraphics, reviewScore, reviewMental, reviewGrade;
        [SerializeField] Image reviewFace;
        [SerializeField] Sprite angryFace, neutralFace, happyFace;
        [SerializeField,Min(1)] float charactersPerSecond=30;
        [SerializeField,Min(0.5f)] float resultDisplaySeconds=3;
        readonly List<int> visibleSteps=new List<int>();
        float elapsed,resultElapsed;
        int lastInputFrame=-1;
        GameObject previousSelection;
        Page page;
        Color defaultResultColor;
        float defaultResultFontMax, defaultResultHeight;
        public bool IsOpen => page != Page.Hidden;
        public bool ShowingPresentation => page == Page.Presentation;
        public bool ShowingResult => page == Page.EventResult || ShowingPresentation;
        public bool IsTyping => page == Page.Event && body.maxVisibleCharacters<body.textInfo.characterCount;
        public bool CanChoose => page == Page.Event && !IsTyping;
        public int VisibleCharacters => body.maxVisibleCharacters;
        public event Action Closed;
        public event Action<bool> ChoiceRequested;
        void Awake()
        {
            defaultResultColor = resultLabel.color;
            defaultResultFontMax = resultLabel.fontSizeMax;
            defaultResultHeight = resultLabel.rectTransform.rect.height;
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
            GameAudio.Play(GameSound.Laptop);
            Open(Page.Event, "EVENT / "+entry.Id, entry.Title, entry.Content);
            Canvas.ForceUpdateCanvases(); body.ForceMeshUpdate();
            var boundaries=StringInfo.ParseCombiningCharacters(entry.Content);
            visibleSteps.Clear(); int visible=0;
            for(int i=0;i<boundaries.Length;i++)
            {
                int end=i+1<boundaries.Length?boundaries[i+1]:entry.Content.Length;
                while(visible<body.textInfo.characterCount && body.textInfo.characterInfo[visible].index<end) visible++;
                visibleSteps.Add(visible);
            }
            elapsed=0; body.maxVisibleCharacters=0;
            SetButtons();
        }
        public void ShowPresentation(GameDevProgress progress, double score, BunkerRules.PresentationGrade grade)
        {
            string progressLine = $"기획 {Percent(progress.Planning)}   프로그래밍 {Percent(progress.Programming)}   그래픽 {Percent(progress.Graphics)}";
            string summary = $"{progressLine}\n평가 점수  {score:0.000}\n팀원 1인당 기본 멘탈  {grade.MentalDelta:+0;-0;0}";
            GameAudio.Play(GameSound.Stamp);
            Open(Page.Presentation, string.Empty, "중간 발표", summary);
            if (presentationSheet)
            {
                reviewPlanning.text = Percent(progress.Planning);
                reviewProgramming.text = Percent(progress.Programming);
                reviewGraphics.text = Percent(progress.Graphics);
                reviewScore.text = $"평가 점수  {score:0.000}";
                reviewMental.text = $"팀원 1인당 기본 멘탈  {grade.MentalDelta:+0;-0;0}";
                reviewGrade.text = grade.Label;
                reviewGrade.color = grade.Color;
                reviewFace.sprite = grade.MentalDelta < 0 ? angryFace : grade.MentalDelta > 0 ? happyFace : neutralFace;
            }
            resultLabel.text = grade.Label;
            resultLabel.color = grade.Color;
            SetButtons();
        }
        static string Percent(float progress) => $"{Mathf.Floor(progress * 10f) / 10f:0.#}%";

        void Open(Page nextPage, string heading, string caption, string content)
        {
            if(IsOpen) throw new InvalidOperationException("Event popup is already open.");
            GameAudio.DuckBunker(true);
            page=nextPage;
            if (presentationSheet)
            {
                presentationSheet.SetActive(ShowingPresentation);
                number.transform.parent.gameObject.SetActive(!ShowingPresentation);
            }
            number.text=heading; title.text=caption; body.richText=false; body.text=content;
            body.maxVisibleCharacters=int.MaxValue;
            resultLabel.text=string.Empty; resultLabel.color=defaultResultColor;
            resultLabel.fontSizeMax = ShowingPresentation ? 56f : defaultResultFontMax;
            resultLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ShowingPresentation ? 72f : defaultResultHeight);
            panel.SetActive(true);
            scroll.StopMovement(); scroll.verticalNormalizedPosition=1;
            lastInputFrame=Time.frameCount;
            if(EventSystem.current) { previousSelection=EventSystem.current.currentSelectedGameObject; EventSystem.current.SetSelectedGameObject(null); }
        }
        void SetButtons()
        {
            confirm.gameObject.SetActive(IsTyping || ShowingResult);
            buttonLabel.text=ShowingResult?"계속":"전체 보기";
            yesButton.gameObject.SetActive(CanChoose); noButton.gameObject.SetActive(CanChoose);
        }
        void Update()
        {
            if (GameOptions.BlocksInput) return;
            if(!IsOpen) return;
            if(page == Page.EventResult)
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
            if(k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || (!ShowingPresentation && k.spaceKey.wasPressedThisFrame)) Confirm();
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
        public void ShowResult(string result, string narrative = null)
        {
            if(page != Page.Event) return;
            bool hasNarrative = !string.IsNullOrWhiteSpace(narrative);
            body.text = hasNarrative ? narrative : result;
            body.maxVisibleCharacters=int.MaxValue;
            resultLabel.text = hasNarrative ? result : string.Empty;
            Canvas.ForceUpdateCanvases();
            scroll.StopMovement(); scroll.verticalNormalizedPosition=1;
            page=Page.EventResult; resultElapsed=0; lastInputFrame=Time.frameCount; SetButtons();
        }
        public void ShowUnavailable(string message) => ShowResult(message);
        public void ShowChoiceError(string message) { resultLabel.text=message; SetButtons(); }
        public void Close()
        {
            if(!IsOpen) return;
            GameAudio.DuckBunker(false);
            page=Page.Hidden; panel.SetActive(false);
            body.text=title.text=number.text=resultLabel.text=string.Empty; visibleSteps.Clear();
            if(EventSystem.current && previousSelection && previousSelection.activeInHierarchy) EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection=null; Closed?.Invoke();
        }
    }
}
