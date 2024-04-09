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
        [SerializeField] private TextDisplay playerNameDisplay = default;
        [SerializeField] private TextDisplay currentScoreDisplay = default;

        [SerializeField] private bool truncateId = true;

        
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
                Debug.Log("Changed name to: " + playerName);
                DisplayPlayerName();
            }
        }
        private string playerName;

        private LeaderboardItem playerScore;

        private void Start()
        {
            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogError("LeaderboardId is empty!");
                gameObject.SetActive(false);
                return;
            }
            playerHighscoreDisplay.SetItem(null);
            Login();
        }

        private async void Login()
        {
            await Init();
        }
        private async Task Init()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                SetPlayerName(AuthenticationService.Instance.PlayerName);
                return;
            }
            else await UnityServices.InitializeAsync();

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("Failed to initialize UnityServices");
                return;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            SetPlayerName(AuthenticationService.Instance.PlayerName);
        }
        public void LoadNameInput()
        {
            nameInput.Show(PlayerName);
        }
        private void DisplayPlayerName()
        {
            nameGUI.text = PlayerName;
            if (!string.IsNullOrEmpty(PlayerName))
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
            if (truncateId)
            {
                PlayerName = RemoveId(name);
            }
            else PlayerName = name;
        }

        public void NameInput_Confirmed(string input)
        {
            UpdatePlayerName(input);
        }
        public async Task UpdatePlayerName(string name)
        {
            var result = await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            if (!string.IsNullOrEmpty(result))
            {
                SetPlayerName(result);
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
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await Init();
            }
            await AddScore(score);
            await Init();
            GetLeaderboardTopScores();
        }
        public async Task AddScore(double score)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await Init();
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
        public async void ShowLeaderboard()
        {
            gameObject.SetActive(true);
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await Init();
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
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await Init();
            }
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);
            scoreDisplayC.SetItems(GenerateLeaderboardItems(scoresResponse.Results));
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
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await Init();
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
            if (truncateId)
            {
                entryPlayerName = RemoveId(leaderboardEntry.PlayerName);
            }
            else entryPlayerName = leaderboardEntry.PlayerName;

            bool isPlayer = AuthenticationService.Instance.PlayerId == leaderboardEntry.PlayerId;

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