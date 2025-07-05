using HexTecGames.Basics.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HexTecGames.LeaderboardSystem
{
    public class ScoreDisplayController : DisplayController<ScoreDisplay, LeaderboardItem>
    {
        void Awake()
        {
            foreach (var display in displaySpawner)
            {
                display.SetItem(null);
            }
        }

        public void UpdatePlayerName(string playerName)
        {
            foreach (var display in displaySpawner)
            {
                if (display.IsHighlighted)
                {
                    display.UpdateName(playerName);
                }
            }
        }
        public override void SetItems(IList<LeaderboardItem> items, bool display = true)
        {
            base.SetItems(items, display);
            gameObject.SetActive(true);
        }
    }
}