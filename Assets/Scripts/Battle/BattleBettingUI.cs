using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleBettingUI : MonoBehaviour
{
    [Header("Groups")]
    public GameObject rootUI;          // 패널(반투명 배경 포함)
    public GameObject infoTexts;       // 결과 화면 텍스트 묶음(Row_Txt)
    public GameObject bettingButtons;  // 버튼 묶음(Row_Btn)

    [Header("승부 후")]
    public TMP_Text txtPredictionResult; // Row_Txt/Txt_Prediction   ← 결과 화면용
    public TMP_Text txtResult;
    public TMP_Text txtReward;
    public TMP_Text txtBalance;
    public TMP_Text txtHint;

    [Header("승부 전")]
    public TMP_Text txtPredictionStart;  // Row_Btn/Txt_Prediction   ← 시작 화면용
    public Button btnWin;
    public Button btnLose;
    public Button btnStart;
 
    [Header("Colors")]
    public Color normalButton = Color.white;
    public Color selectedButton = new Color(0.25f, 0.75f, 0.35f, 1f); // 초록
    public Color warningTextColor = new Color(1f, 0.3f, 0.2f, 1f);

    [Header("Bet Settings")]
    public int betAmount = 100;

    private enum Prediction { None, Win, Lose }
    private Prediction prediction = Prediction.None;

    private bool battleRunning = false;
    private bool waitingForResultClose = false;
    private bool finishedOnce = false;

    // 버튼 색을 바꿀 대상(스프라이트 안 바꿈)
    private Graphic winGraphic;
    private Graphic loseGraphic;

    // 레퍼런스는 FindFirstObjectByType로 얻음
    private AutoBattleTest battle;
    private GameWallet wallet;

    void Awake()
    {
        battle = FindFirstObjectByType<AutoBattleTest>(FindObjectsInactive.Exclude);
        wallet = FindFirstObjectByType<GameWallet>(FindObjectsInactive.Include);
    }

    void Start()
    {
        btnWin.onClick.AddListener(() => SetPrediction(Prediction.Win));
        btnLose.onClick.AddListener(() => SetPrediction(Prediction.Lose));
        btnStart.onClick.AddListener(OnClickStart);

        winGraphic  = btnWin.targetGraphic;
        loseGraphic = btnLose.targetGraphic;

        // 버튼 호버가 덮어쓰지 않도록 ColorBlock 통일
        LockButtonColor(btnWin, normalButton);
        LockButtonColor(btnLose, normalButton);

        ShowButtonsOnly();
        ResetSelectionVisuals();
        UpdateStartTexts();     // 시작 화면 텍스트 갱신
        UpdateResultTexts(true); // 결과 텍스트 초기화
    }

    void Update()
    {
        if (waitingForResultClose && Input.GetKeyDown(KeyCode.E))
        {
            waitingForResultClose = false;
            finishedOnce = true;
            if (rootUI) rootUI.SetActive(false);
        }
    }

    // -------- UI 상태 전환 --------
    private void ShowButtonsOnly()
    {
        rootUI?.SetActive(true);
        bettingButtons?.SetActive(true);
        infoTexts?.SetActive(false);
        txtHint?.gameObject.SetActive(false);
    }

    private void ShowResultOnly()
    {
        rootUI?.SetActive(true);
        bettingButtons?.SetActive(false);
        infoTexts?.SetActive(true);
        txtHint?.gameObject.SetActive(true);
        if (txtHint) txtHint.text = "E 키를 눌러 종료";
    }

    // -------- 버튼/예측 처리 --------
    private void SetPrediction(Prediction p)
    {
        if (battleRunning || finishedOnce) return;

        prediction = p;
        UpdateStartTexts();     // 시작 화면 텍스트 즉시 반영
        UpdateButtonColors();   // 라디오처럼 한쪽만 초록
    }

    private void OnClickStart()
    {
        if (battleRunning || finishedOnce) return;

        if (prediction == Prediction.None)
        {
            // 경고: 시작 화면의 예측 텍스트에만 띄움
            if (txtPredictionStart)
            {
                txtPredictionStart.color = warningTextColor;
                txtPredictionStart.text = "결과 예측을 선택해주세요";
            }
            return;
        }

        if (!battle) battle = FindFirstObjectByType<AutoBattleTest>();
        if (!wallet) wallet = FindFirstObjectByType<GameWallet>(FindObjectsInactive.Include);
        if (!battle || !wallet)
        {
            ShowResultOnly();
            txtResult.text = !battle ? "결과: AutoBattleTest 없음" : "결과: GameWallet 없음";
            return;
        }

        battleRunning = true;
        rootUI?.SetActive(false); // 전투 중 UI 전체 숨김
        battle.RunOneBattle(OnBattleEnd);
    }

    private void OnBattleEnd(Team winner)
    {
        battleRunning = false;

        bool predictedAllyWin = (prediction == Prediction.Win);
        bool allyWon = (winner == Team.Ally);
        bool predictSuccess = (predictedAllyWin == allyWon);
        int delta = predictSuccess ? betAmount : -betAmount;

        wallet.Add(delta);

        // 전투 유닛 정리(이제 public Cleanup 사용)
        battle.Cleanup();

        // 결과만 표시
        ShowResultOnly();
        UpdateResultTexts(false, predictSuccess, allyWon, delta);

        // 다음을 대비해 시작 화면 선택 비주얼 초기화
        prediction = Prediction.None;
        ResetSelectionVisuals();
        UpdateStartTexts();

        waitingForResultClose = true; // E 키로 종료
    }

    // -------- 텍스트 업데이트 --------
    private void UpdateStartTexts()
    {
        if (!txtPredictionStart) return;

        txtPredictionStart.color = Color.white;
        txtPredictionStart.text = prediction switch
        {
            Prediction.Win  => "예측: Win",
            Prediction.Lose => "예측: Lose",
            _               => "예측: -"
        };
    }

    private void UpdateResultTexts(bool clear,
        bool predictSuccess = false, bool allyWon = false, int delta = 0)
    {
        if (clear)
        {
            if (txtPredictionResult) txtPredictionResult.text = "예측: -";
            if (txtResult)          txtResult.text          = "결과: -";
            if (txtReward)          txtReward.text          = "보상: -";
            if (txtBalance)         txtBalance.text         = "잔액: " + (wallet ? wallet.Coins : 0);
            return;
        }

        if (txtPredictionResult) txtPredictionResult.text = "예측: " + (predictSuccess ? "성공" : "실패");
        if (txtResult)           txtResult.text           = "결과: " + (allyWon ? "아군 승" : "적군 승");
        if (txtReward)           txtReward.text           = "보상: " + (delta >= 0 ? "+" : "") + delta;
        if (txtBalance)          txtBalance.text          = "잔액: " + (wallet ? wallet.Coins : 0);
    }

    // -------- 버튼 색 처리(스프라이트 교체 없이 color만) --------
    private void ResetSelectionVisuals()
    {
        if (winGraphic)  winGraphic.color  = normalButton;
        if (loseGraphic) loseGraphic.color = normalButton;
        LockButtonColor(btnWin,  normalButton);
        LockButtonColor(btnLose, normalButton);
    }

    private void UpdateButtonColors()
    {
        Color winColor  = (prediction == Prediction.Win)  ? selectedButton : normalButton;
        Color loseColor = (prediction == Prediction.Lose) ? selectedButton : normalButton;

        if (winGraphic)  winGraphic.color  = winColor;
        if (loseGraphic) loseGraphic.color = loseColor;

        LockButtonColor(btnWin,  winColor);
        LockButtonColor(btnLose, loseColor);
    }

    private void LockButtonColor(Button btn, Color c)
    {
        if (!btn) return;
        var cb = btn.colors;
        cb.normalColor = cb.highlightedColor = cb.pressedColor = cb.selectedColor = c;
        cb.disabledColor = c * 0.6f;
        cb.colorMultiplier = 1f;
        btn.colors = cb;
        // 필요하면 완전 고정: btn.transition = Selectable.Transition.None;
    }
}
