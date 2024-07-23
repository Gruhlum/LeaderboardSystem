using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using HexTecGames.Basics.UI.Displays;
using System.Threading.Tasks;
using Unity.Services.Leaderboards.Models;
using TMPro;
using HexTecGames.Basics.UI;
using HexTecGames.Basics;

namespace HexTecGames.LeaderboardSystem
{
    public class UnityLeaderboardManager : MonoBehaviour
    {
        [SerializeField] private string leaderboardId = default;
        [SerializeField] private int entriesPerPage = 10;
        [SerializeField] private ScoreDisplayController scoreDisplayC = default;
        [SerializeField] private ScoreDisplay playerHighscoreDisplay = default;
        [SerializeField] private InputDisplay nameInput = default;
        [SerializeField] private GameObject ShowPlayerScoreBtn = default;
        [SerializeField] private TMP_Text nameGUI = default;
        [SerializeField] private TMP_Text loadingText = default;
        [SerializeField] private TextDisplay playerNameDisplay = default;
        [SerializeField] private TextDisplay currentScoreDisplay = default;

        [Header("Name Settings")]
        [SerializeField] private bool truncateId = true;
        public bool CensorPlayerName
        {
            get
            {
                return censorPlayerName;
            }
            set
            {
                censorPlayerName = value;
            }
        }
        [SerializeField] private bool censorPlayerName = default;
        [DrawIf(nameof(censorPlayerName), true)][SerializeField] private string playerNameOverride = "You";
        public bool CensorOtherNames
        {
            get
            {
                return censorOtherNames;
            }
            set
            {
                censorOtherNames = value;
            }
        }
        [SerializeField] private bool censorOtherNames = default;
        [DrawIf(nameof(censorOtherNames), true)][SerializeField] private string otherNameOverride = "Player";

        private bool triedInit;
        private bool initSuccess;
        private int currentPage;
        private int maximumPages = -1;

        public LeaderboardItem PlayerItem
        {
            get
            {
                return this.playerScore;
            }
            private set
            {
                this.playerScore = value;
                ShowPlayerScoreBtn.SetActive(this.playerScore != null);
            }
        }

        public string PlayerName
        {
            get
            {
                return playerName;
            }
            private set
            {
                if (playerName == value)
                {
                    return;
                }
                playerName = value;
                DisplayPlayerName();
            }
        }
        private string playerName;

        private LeaderboardItem playerScore;

        /* Only sign in OnEnable
         * if it doesn't work show a message and turn a bool to false
         * check that bool for every other call
         * 
         * 
         */


        private void Awake()
        {
            if (playerNameDisplay != null)
            {
                playerNameDisplay.SetText(string.Empty);
            }
        }
        void OnDisable()
        {
            triedInit = false;
        }
        [ContextMenu("Delete Player Account")]
        public async void DeletePlayerAccount()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("Can only delete in PlayMode");
                return;
            }
            if (!initSuccess)
            {
                return;
            }
            await AuthenticationService.Instance.DeleteAccountAsync();
        }
        private async Task Init()
        {
            if (triedInit)
            {
                return;
            }
            triedInit = true;

            if (playerNameDisplay != null)
            {
                playerNameDisplay.gameObject.SetActive(!censorPlayerName);
            }
            loadingText.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogError("LeaderboardId is empty!");
                gameObject.SetActive(false);
                return;
            }
            if (playerHighscoreDisplay != null)
            {
                playerHighscoreDisplay.SetItem(null);
            }

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("Trying to initialize UnityServices");
                await UnityServices.InitializeAsync();
            }
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("Initialization failed!");
                return;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    Debug.Log("Attempting to sign in");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    initSuccess = true;
                    Debug.Log("sign in success");
                    loadingText.gameObject.SetActive(false);
                }
                catch (System.Exception e)
                {
                    loadingText.text = "Could not connect!";
                    Debug.Log("Error when signing in: " + e.Message);
                }
            }
            else
            {
                initSuccess = true;
                loadingText.gameObject.SetActive(false);
            }

            if (AuthenticationService.Instance.PlayerName != null)
            {
                SetPlayerName(AuthenticationService.Instance.PlayerName);
            }
        }
        public void LoadNameInput()
        {
            nameInput.Show(PlayerName);
        }
        private void DisplayPlayerName()
        {
            nameGUI.text = PlayerName;
            if (!censorPlayerName && !string.IsNullOrEmpty(PlayerName))
            {
                playerNameDisplay.SetText(PlayerName);
            }
            scoreDisplayC.UpdatePlayerName(PlayerName);
            playerHighscoreDisplay.UpdateName(PlayerName);
            if (PlayerItem != null)
            {
                PlayerItem.name = PlayerName;
            }
        }
        public void SetPlayerName(string name)
        {
            if (censorPlayerName)
            {
                PlayerName = playerNameOverride;
            }
            else if (truncateId)
            {
                PlayerName = RemoveId(name);
            }
            else PlayerName = name;
        }

        public void NameInput_Confirmed(string input)
        {
            ProcessNameInput(input);
        }
        public async void ProcessNameInput(string input)
        {
            await UpdatePlayerName(input);
        }
        public async Task UpdatePlayerName(string name)
        {
            if (!await AllowCalls())
            {
                return;
            }
            try
            {
                var result = await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
                Debug.Log("Changing Player name: " + result);

                if (!string.IsNullOrEmpty(result))
                {
                    SetPlayerName(result);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.ToString());
                return;
            }
            

            
        }

        private string RemoveId(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            int index = input.IndexOf('#');
            if (index <= 0)
            {
                return input;
            }
            return input.Substring(0, index);
        }
        public async Task<string> GetPlayerName()
        {
            var result = await AuthenticationService.Instance.GetPlayerNameAsync();
            Debug.Log("playername is " + result);
            return result;
        }
        public async void RetrieveAndDisplayScore(int score)
        {
            gameObject.SetActive(true);
            if (!await AllowCalls())
            {
                return;
            }
            await AddScore(score);
            GetLeaderboardTopScores();
        }
        public async Task AddScore(double score)
        {
            if (!await AllowCalls())
            {
                return;
            }
            var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            PlayerItem = GenerateLeaderboardItem(entry);
            playerHighscoreDisplay.SetItem(PlayerItem);
            if (entry.Score > score)
            {
                currentScoreDisplay.SetText(score.ToString());
            }
            playerHighscoreDisplay.gameObject.SetActive(true);
        }
        private async Task<bool> AllowCalls()
        {
            if (initSuccess)
            {
                return true;
            }
            if (!triedInit)
            {
                await Init();
            }
            return initSuccess;
        }
        public async void ShowLeaderboard()
        {
            gameObject.SetActive(true);
            if (!await AllowCalls())
            {
                return;
            }
            var playerScore = await GetPlayerScore();
            if (playerScore != null)
            {
                playerHighscoreDisplay.SetItem(PlayerItem);
            }
            GetLeaderboardTopScores();
        }
        public async void GetLeaderboardTopScores()
        {
            if (!await AllowCalls())
            {
                return;
            }
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);
            scoreDisplayC.SetItems(GenerateLeaderboardItems(scoresResponse.Results));
            currentPage = 0;
        }
        public void AddTestScore(int score)
        {
            RetrieveAndDisplayScore(score);
        }
        public void GetNextPage()
        {
            if (maximumPages != -1 && currentPage + 1 >= maximumPages)
            {
                return;
            }
            GetLeaderboardPage(currentPage + 1);
        }
        public void GetPreviousPage()
        {
            if (currentPage <= 0)
            {
                return;
            }
            GetLeaderboardPage(currentPage - 1);
        }
        public async Task<LeaderboardItem> GetPlayerScore()
        {
            if (!await AllowCalls())
            {
                return null;
            }
            try
            {
                var scoresResponse = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
                if (scoresResponse == null)
                {
                    Debug.Log("failed to get score response");
                    return null;
                }
                PlayerItem = GenerateLeaderboardItem(scoresResponse);
                return PlayerItem;
            }
            catch (System.Exception ex)
            {
                Debug.Log(ex.Message);
                return null;
            }
        }
        public async void GetLeaderboardPage(int page)
        {
            if (!await AllowCalls())
            {
                return;
            }
            currentPage = page;
            var scoreResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId,
                new GetScoresOptions { Offset = page * entriesPerPage, Limit = entriesPerPage });

            if (scoreResponse != null && scoreResponse.Results != null && scoreResponse.Results.Count > 0)
            {
                DisplayLeaderboardResults(scoreResponse.Results);

                maximumPages = (scoreResponse.Total + entriesPerPage - 1) / entriesPerPage;

                if (currentPage >= maximumPages)
                {
                    currentPage = maximumPages;
                }
            }
        }
        public async void DisplayPlayerPage()
        {
            if (!await AllowCalls())
            {
                return;
            }
            var score = await GetPlayerScore();
            if (score == null)
            {
                return;
            }
            int playerPage = (score.rank - 1) / entriesPerPage;
            GetLeaderboardPage(playerPage);
        }
        private void DisplayLeaderboardResults(List<LeaderboardEntry> leaderboardEntries)
        {
            if (leaderboardEntries == null || leaderboardEntries.Count <= 0)
            {
                return;
            }
            if (leaderboardEntries.Count > entriesPerPage)
            {
                for (int i = leaderboardEntries.Count - 1; i >= entriesPerPage; i--)
                {
                    leaderboardEntries.RemoveAt(i);
                }
            }
            List<LeaderboardItem> leaderboardItems = GenerateLeaderboardItems(leaderboardEntries);
            scoreDisplayC.SetItems(leaderboardItems);
        }
        public LeaderboardItem GenerateLeaderboardItem(LeaderboardEntry leaderboardEntry)
        {
            string entryPlayerName;

            bool isPlayer = AuthenticationService.Instance.PlayerId == leaderboardEntry.PlayerId;

            if (isPlayer && censorPlayerName)
            {
                entryPlayerName = playerNameOverride;
            }
            else if (!isPlayer && censorOtherNames)
            {
                entryPlayerName = $"{otherNameOverride}{Random.Range(1000, 10000)}";
            }
            else if (truncateId)
            {
                entryPlayerName = RemoveId(leaderboardEntry.PlayerName);
            }
            else entryPlayerName = leaderboardEntry.PlayerName;


            return new LeaderboardItem(entryPlayerName, leaderboardEntry.Rank + 1, (int)leaderboardEntry.Score, isPlayer);
        }
        public List<LeaderboardItem> GenerateLeaderboardItems(List<LeaderboardEntry> leaderboardEntries)
        {
            List<LeaderboardItem> results = new List<LeaderboardItem>();
            foreach (var leaderboardEntry in leaderboardEntries)
            {
                results.Add(GenerateLeaderboardItem(leaderboardEntry));
            }
            return results;
        }
    }
}