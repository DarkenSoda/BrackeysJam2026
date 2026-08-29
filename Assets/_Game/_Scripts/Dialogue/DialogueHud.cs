using System.Collections;
using System.Text;
using UnityEngine;
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

    [Header("Group these under ONE parent object: SpeakerText + BodyText + Portrait " +
            "(NOT scratchpad — scratchpad must stay visible independently per spec)")]
    [SerializeField] private GameObject dialoguePanel;

    private bool waitingOnFlash;
    private RolledTip shownTip;
    private GameClock clock;

    private TripState? pendingRevealState;

    private void OnEnable()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged += HandleState;
            TripController.Instance.OnOutcomeResolved += HandleOutcome;
        }

        if (DrivingController.Instance != null)
            DrivingController.Instance.OnArrivedAtStop += HandleCarArrived;

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

        if (DrivingController.Instance != null)
        {
            DrivingController.Instance.OnArrivedAtStop -= HandleCarArrived;
            DrivingController.Instance.OnArrivedAtStop += HandleCarArrived;
        }

        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged -= HandleState;
            TripController.Instance.OnTripStateChanged += HandleState;
            TripController.Instance.OnOutcomeResolved -= HandleOutcome;
            TripController.Instance.OnOutcomeResolved += HandleOutcome;

            HandleState(TripController.Instance.CurrentState);
        }
        else
        {
            Debug.LogWarning("[DialogueHud] TripController.Instance is null at Start().");
        }
    }

    private void OnDisable()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged -= HandleState;
            TripController.Instance.OnOutcomeResolved -= HandleOutcome;
        }

        if (DrivingController.Instance != null)
            DrivingController.Instance.OnArrivedAtStop -= HandleCarArrived;

        if (clock != null)
            clock.OnTimeChanged -= RefreshClock;
    }

    private void HandleState(TripState state)
    {
        HideFlash();
        StopAllCoroutines();
        waitingOnFlash = false;
        pendingRevealState = null;
        RefreshClock();

        switch (state)
        {
            case TripState.PICKUP:
            case TripState.ENCOUNTER:
                HideDialogueContent();
                ShowButtons(cont: false, follow: false, ignore: false);
                pendingRevealState = state;
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

    private void HandleCarArrived()
    {
        if (pendingRevealState == null) return;
        if (TripController.Instance == null || TripController.Instance.CurrentState != pendingRevealState.Value) return;

        TripState reveal = pendingRevealState.Value;
        pendingRevealState = null;

        if (reveal == TripState.PICKUP)
            ShowPickup();
        else if (reveal == TripState.ENCOUNTER)
            ShowTip();
    }

    private void ShowPickup()
    {
        RolledTrip trip = TripController.Instance.CurrentTrip;
        shownTip = null;
        SetPanelVisible(true);
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
        SetPanelVisible(true);
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
        SetPanelVisible(true);
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
        // We leave the portrait field assigned in the Inspector to avoid errors, 
        // but we force it to stay disabled so it never spawns in the HUD.
        if (portrait != null)
        {
            portrait.enabled = false;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(visible);
    }

    private void HideDialogueContent()
    {
        SetPanelVisible(false);
        if (speakerText != null) speakerText.text = "";
        if (bodyText != null) bodyText.text = "";
        SetPortrait(null);
    }

    private void ShowButtons(bool cont, bool follow, bool ignore)
    {
        if (continueButton != null) continueButton.gameObject.SetActive(cont);
        if (followButton != null) followButton.gameObject.SetActive(follow);
        if (ignoreButton != null) ignoreButton.gameObject.SetActive(ignore);
    }

    public void OnContinue()
    {
        if (TripController.Instance.CurrentState != TripState.PICKUP) return;

        HideDialogueContent();
        ShowButtons(cont: false, follow: false, ignore: false);
        TripController.Instance.BeginEncounters();
    }

    public void OnFollow()
    {
        if (waitingOnFlash) return;
        StartCoroutine(CommitChoice(true));
    }

    public void OnIgnore()
    {
        if (waitingOnFlash) return;
        StartCoroutine(CommitChoice(false));
    }

    private IEnumerator CommitChoice(bool followed)
    {
        waitingOnFlash = true;
        HideDialogueContent();
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
}