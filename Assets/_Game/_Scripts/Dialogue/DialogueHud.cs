using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class DialogueHud : MonoBehaviour
{
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text scratchpadText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button followButton;
    [SerializeField] private Button ignoreButton;
    [SerializeField] private Image flash;
    [SerializeField] private Image portrait;
    [SerializeField] private Text clockText;

    private bool built;
    private bool waitingOnFlash;
    private RolledTip shownTip;
    private GameClock clock;

    private void Awake()
    {
        if (speakerText == null)
            BuildFallbackCanvas();
    }

    private void OnEnable()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged += HandleState;
            TripController.Instance.OnOutcomeResolved += HandleOutcome;
        }

        clock = GameClock.Instance;
        if (clock != null)
            clock.OnTimeChanged += RefreshClock;
        RefreshClock();
    }

    private void Start()
    {
        clock = GameClock.Instance;
        if (clock != null)
        {
            clock.OnTimeChanged -= RefreshClock;
            clock.OnTimeChanged += RefreshClock;
        }
        RefreshClock();
        if (TripController.Instance != null)
            HandleState(TripController.Instance.CurrentState);
    }

    private void OnDisable()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged -= HandleState;
            TripController.Instance.OnOutcomeResolved -= HandleOutcome;
        }

        if (clock != null)
            clock.OnTimeChanged -= RefreshClock;
    }

    private void HandleState(TripState state)
    {
        HideFlash();
        StopAllCoroutines();
        waitingOnFlash = false;
        RefreshClock();

        switch (state)
        {
            case TripState.PICKUP:
                ShowPickup();
                break;
            case TripState.ENCOUNTER:
                ShowTip();
                break;
            case TripState.DROPOFF:
                ShowDropoff();
                break;
        }

        if (AudioManager.Instance != null && 
            (state == TripState.PICKUP || state == TripState.ENCOUNTER || state == TripState.DROPOFF))
        {
            AudioManager.Instance.PlayDialogueMuffle();
        }
    }

    private void ShowPickup()
    {
        RolledTrip trip = TripController.Instance.CurrentTrip;
        shownTip = null;
        SetSpeaker("Passenger");
        bodyText.text = trip != null ? trip.customerLine : "...";
        RefreshScratchpad(trip);
        SetPortrait(null);
        ShowButtons(cont: true, follow: false, ignore: false);
    }

    private void ShowTip()
    {
        RolledTip tip = TripController.Instance.CurrentTip;
        RolledTrip trip = TripController.Instance.CurrentTrip;
        shownTip = tip;
        SetSpeaker(tip != null && tip.giver != null ? tip.giver.displayName : "Someone");
        bodyText.text = tip != null ? tip.line : "...";
        RefreshScratchpad(trip);
        SetPortrait(tip != null && tip.giver != null ? tip.giver.truthSprite : null);
        ShowButtons(cont: false, follow: true, ignore: true);
        HideFlash();
        RefreshClock();
    }

    private void ShowDropoff()
    {
        RolledTrip trip = TripController.Instance.CurrentTrip;
        shownTip = null;
        SetSpeaker("Passenger");

        if (TripController.Instance.ShiftOver)
            bodyText.text = "that's dawn. park it.";
        else
            bodyText.text = trip != null ? trip.dropoffLine : "we're here.";

        RefreshScratchpad(trip);
        SetPortrait(null);
        ShowButtons(cont: false, follow: false, ignore: false);

        if (!TripController.Instance.ShiftOver)
            StartCoroutine(NextFareSoon());
    }

    private void RefreshScratchpad(RolledTrip trip)
    {
        if (scratchpadText == null) return;
        if (trip == null || trip.scratchpad == null)
        {
            scratchpadText.text = "";
            return;
        }

        var sb = new StringBuilder();
        sb.Append("scratchpad\n");
        for (int i = 0; i < trip.scratchpad.Count; i++)
            sb.Append("- ").Append(trip.scratchpad[i]).Append('\n');
        scratchpadText.text = sb.ToString();
    }

    private void HideFlash()
    {
        if (flash != null)
            flash.gameObject.SetActive(false);
    }

    private void RefreshClock()
    {
        if (clockText == null) return;
        if (clock == null)
            clock = GameClock.Instance;
        clockText.text = clock != null ? clock.ClockLabel : "";
    }

    private void SetSpeaker(string name)
    {
        if (speakerText != null)
            speakerText.text = name;
    }

    private void SetPortrait(Sprite sprite)
    {
        if (portrait == null) return;
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
    }

    private void ShowButtons(bool cont, bool follow, bool ignore)
    {
        if (continueButton != null) continueButton.gameObject.SetActive(cont);
        if (followButton != null) followButton.gameObject.SetActive(follow);
        if (ignoreButton != null) ignoreButton.gameObject.SetActive(ignore);
    }

    private void OnContinue()
    {
        if (TripController.Instance.CurrentState == TripState.PICKUP)
            TripController.Instance.BeginEncounters();
    }

    private void OnFollow()
    {
        if (waitingOnFlash) return;
        StartCoroutine(CommitChoice(true));
    }

    private void OnIgnore()
    {
        if (waitingOnFlash) return;
        StartCoroutine(CommitChoice(false));
    }

    private IEnumerator CommitChoice(bool followed)
    {
        waitingOnFlash = true;
        ShowButtons(cont: false, follow: false, ignore: false);

        RolledTip tip = shownTip != null ? shownTip : TripController.Instance.CurrentTip;
        PlayChoiceAudio(followed, tip);

        if (tip != null && !tip.isTruth && tip.giver != null)
            SetPortrait(tip.giver.lieSprite);

        TripController.Instance.LearnFact(tip);
        RefreshScratchpad(TripController.Instance.CurrentTrip);

        yield return new WaitForSeconds(0.55f);
        waitingOnFlash = false;
        TripController.Instance.ResolveChoice(followed);
    }

    private void PlayChoiceAudio(bool followed, RolledTip tip)
    {
        if (AudioManager.Instance == null) return;

        bool tipWasTruth = tip == null || tip.isTruth;
        bool correct = followed == tipWasTruth;

        if (correct)
            AudioManager.Instance.PlayCorrectChoice();
        else
            AudioManager.Instance.PlayWrongChoice();
    }

    private void HandleOutcome(bool correct)
    {
        Color c = correct ? new Color(0.2f, 0.85f, 0.3f, 0.45f) : new Color(0.9f, 0.15f, 0.15f, 0.45f);
        StartCoroutine(Flash(c));
    }

    private IEnumerator Flash(Color c)
    {
        if (flash == null) yield break;
        waitingOnFlash = true;
        flash.color = c;
        flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.25f);
        flash.gameObject.SetActive(false);
        waitingOnFlash = false;
    }

    private IEnumerator NextFareSoon()
    {
        yield return new WaitForSeconds(1.4f);
        if (TripController.Instance != null && !TripController.Instance.ShiftOver)
            TripController.Instance.TryStartNextFare();
    }

    private void BuildFallbackCanvas()
    {
        if (built) return;
        built = true;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        var canvasGo = new GameObject("DialogueCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        scratchpadText = MakeText(canvasGo.transform, "Scratchpad", new Vector2(0.02f, 0.55f), new Vector2(0.28f, 0.96f), 22, TextAnchor.UpperLeft, font);
        clockText = MakeText(canvasGo.transform, "Clock", new Vector2(0.32f, 0.90f), new Vector2(0.72f, 0.99f), 32, TextAnchor.MiddleCenter, font);
        clockText.fontStyle = FontStyle.Bold;
        speakerText = MakeText(canvasGo.transform, "Speaker", new Vector2(0.32f, 0.78f), new Vector2(0.72f, 0.88f), 26, TextAnchor.LowerLeft, font);
        speakerText.fontStyle = FontStyle.Bold;
        bodyText = MakeText(canvasGo.transform, "Body", new Vector2(0.32f, 0.22f), new Vector2(0.72f, 0.78f), 22, TextAnchor.UpperLeft, font);

        var portraitGo = new GameObject("Portrait");
        portraitGo.transform.SetParent(canvasGo.transform, false);
        portrait = portraitGo.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.color = Color.white;
        Stretch(portraitGo.GetComponent<RectTransform>(), new Vector2(0.74f, 0.22f), new Vector2(0.98f, 0.88f));
        portrait.enabled = false;

        continueButton = MakeButton(canvasGo.transform, "Continue", "continue", new Vector2(0.72f, 0.08f), new Vector2(0.96f, 0.18f), font, OnContinue);
        followButton = MakeButton(canvasGo.transform, "Follow", "follow", new Vector2(0.32f, 0.08f), new Vector2(0.54f, 0.18f), font, OnFollow);
        ignoreButton = MakeButton(canvasGo.transform, "Ignore", "ignore", new Vector2(0.56f, 0.08f), new Vector2(0.70f, 0.18f), font, OnIgnore);

        var flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(canvasGo.transform, false);
        flash = flashGo.AddComponent<Image>();
        flash.color = Color.clear;
        Stretch(flashGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        flash.raycastTarget = false;
        flashGo.SetActive(false);
        flashGo.transform.SetAsLastSibling();
    }

    private static Text MakeText(Transform parent, string name, Vector2 min, Vector2 max, int size, TextAnchor align, Font font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(go.GetComponent<RectTransform>(), min, max);
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Font font, UnityEngine.Events.UnityAction click)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(click);
        Stretch(go.GetComponent<RectTransform>(), min, max);

        var child = new GameObject("Label");
        child.transform.SetParent(go.transform, false);
        var t = child.AddComponent<Text>();
        t.font = font;
        t.fontSize = 24;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
        Stretch(child.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        return btn;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
