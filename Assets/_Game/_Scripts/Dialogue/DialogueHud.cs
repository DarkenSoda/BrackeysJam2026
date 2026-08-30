using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueHud : MonoBehaviour
{
    [Header("Scratchpad")]
    [SerializeField] private TextMeshProUGUI scratchpadText;

    [Header("Clock")]
    [SerializeField] private TextMeshProUGUI clockHoursText;
    [SerializeField] private TextMeshProUGUI clockMinutesText;
    [SerializeField] private TextMeshProUGUI clockAmPmText;

    [Header("NPC Dialogue")]
    [SerializeField] private GameObject npcDialogueParent;
    [SerializeField] private TextMeshProUGUI npcSpeakerText;
    [SerializeField] private TextMeshProUGUI npcBodyText;
    [SerializeField] private Image npcPortrait;
    [SerializeField] private Button followButton;
    [SerializeField] private Button ignoreButton;

    [Header("Passenger Dialogue")]
    [SerializeField] private GameObject passengerDialogueParent;
    [SerializeField] private TextMeshProUGUI passengerSpeakerText;
    [SerializeField] private TextMeshProUGUI passengerBodyText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Image passengerImage;

    [Header("Effects")]
    [SerializeField] private Image flash;

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

            RefreshScratchpad(null);
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

        SetPassengerDialogueVisible(true);
        SetNPCDialogueVisible(false);

        if (passengerSpeakerText != null) passengerSpeakerText.text = "Passenger";
        if (passengerBodyText != null) passengerBodyText.text = trip != null ? trip.customerLine : "...";

        RefreshScratchpad(trip);
        ShowButtons(cont: true, follow: false, ignore: false);
        if (AudioManager.Instance != null) AudioManager.Instance.SetDialogueDucking(true);

        // Play the door open/pickup sound exactly when the passenger gets in!
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPickup();
        }
        passengerImage.gameObject.SetActive(true);
    }
    private void ShowTip()
    {
        RolledTip tip = TripController.Instance.CurrentTip;
        RolledTrip trip = TripController.Instance.CurrentTrip;
        shownTip = tip;

        SetPassengerDialogueVisible(false);
        SetNPCDialogueVisible(true);

        if (npcSpeakerText != null) npcSpeakerText.text = tip != null && tip.giver != null ? tip.giver.displayName : "Someone";
        if (npcBodyText != null) npcBodyText.text = tip != null ? tip.line : "...";
        SetPortrait(tip != null && tip.giver != null ? tip.giver.truthSprite : null);

        RefreshScratchpad(trip);
        ShowButtons(cont: false, follow: true, ignore: true);
        HideFlash();
        RefreshClock();
        if (AudioManager.Instance != null) AudioManager.Instance.SetDialogueDucking(true);

        if (tip != null && tip.giver != null && tip.giver.spawnSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNPC(tip.giver.spawnSound, tip.giver.spawnVolume);
        }
    }

    private void ShowDropoff()
    {
        RolledTrip trip = TripController.Instance.CurrentTrip;
        shownTip = null;

        SetPassengerDialogueVisible(true);
        SetNPCDialogueVisible(false);

        if (passengerSpeakerText != null) passengerSpeakerText.text = "Passenger";

        if (passengerBodyText != null)
        {
            if (TripController.Instance.ShiftOver)
                passengerBodyText.text = "that's dawn. park it.";
            else
                passengerBodyText.text = trip != null ? trip.dropoffLine : "we're here.";
        }

        RefreshScratchpad(trip);
        ShowButtons(cont: true, follow: false, ignore: false);
        if (AudioManager.Instance != null) AudioManager.Instance.SetDialogueDucking(true);

        // Play the door close/dropoff sound exactly when they leave!
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDropoff();
        }
        passengerImage.gameObject.SetActive(false);

    }
    private void RefreshScratchpad(RolledTrip trip)
    {
        if (scratchpadText == null) return;

        var sb = new StringBuilder();
        sb.Append("Scratchpad\n\n");

        if (trip != null && trip.scratchpad != null)
        {
            for (int i = 0; i < trip.scratchpad.Count; i++)
                sb.Append("- ").Append(trip.scratchpad[i]).Append('\n');
        }

        scratchpadText.text = sb.ToString();
    }

    private void HideFlash()
    {
        if (flash != null)
            flash.gameObject.SetActive(false);
    }

    private void RefreshClock()
    {
        if (clock == null)
            clock = GameClock.Instance;

        if (clockHoursText != null)
            clockHoursText.text = clock != null ? clock.ClockHours : "";

        if (clockMinutesText != null)
            clockMinutesText.text = clock != null ? clock.ClockMinutes : "";

        if (clockAmPmText != null)
            clockAmPmText.text = clock != null ? clock.ClockAmPm : "";
    }

    private void SetPortrait(Sprite sprite)
    {
        if (npcPortrait == null) return;

        if (sprite != null)
        {
            npcPortrait.sprite = sprite;
            npcPortrait.enabled = true;
        }
        else
        {
            npcPortrait.enabled = false;
        }
    }

    private void SetNPCDialogueVisible(bool visible)
    {
        if (npcDialogueParent != null)
            npcDialogueParent.SetActive(visible);
    }

    private void SetPassengerDialogueVisible(bool visible)
    {
        if (passengerDialogueParent != null)
            passengerDialogueParent.SetActive(visible);
    }

    private void HideDialogueContent()
    {
        SetNPCDialogueVisible(false);
        SetPassengerDialogueVisible(false);

        if (npcSpeakerText != null) npcSpeakerText.text = "";
        if (npcBodyText != null) npcBodyText.text = "";
        if (passengerSpeakerText != null) passengerSpeakerText.text = "";
        if (passengerBodyText != null) passengerBodyText.text = "";
        SetPortrait(null);
        if (AudioManager.Instance != null) AudioManager.Instance.SetDialogueDucking(false);
    }

    private void ShowButtons(bool cont, bool follow, bool ignore)
    {
        if (continueButton != null) continueButton.gameObject.SetActive(cont);
        if (followButton != null) followButton.gameObject.SetActive(follow);
        if (ignoreButton != null) ignoreButton.gameObject.SetActive(ignore);
    }

    public void OnContinue()
    {
        HideDialogueContent();
        ShowButtons(cont: false, follow: false, ignore: false);

        if (TripController.Instance.CurrentState == TripState.PICKUP)
        {
            TripController.Instance.BeginEncounters();
        }
        else if (TripController.Instance.CurrentState == TripState.DROPOFF)
        {
            RefreshScratchpad(null);

            if (!TripController.Instance.ShiftOver)
            {
                TripController.Instance.TryStartNextFare();
            }
            else
            {
                GameFlowController gfc = FindAnyObjectByType<GameFlowController>();
                if (gfc != null) gfc.TriggerEndShift();
            }
        }
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
        {
            AudioManager.Instance.PlayCorrectChoice();
            if (ShockWaveManager.Instance != null)
                ShockWaveManager.Instance.PlayCorrectVFX();
        }

        else
        {
            AudioManager.Instance.PlayWrongChoice();
            if (ShockWaveManager.Instance != null)
                ShockWaveManager.Instance.PlayWrongVFX();
        }
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
        // This is no longer used, as we rely on OnContinue for dropoffs now
        yield break;
    }
}