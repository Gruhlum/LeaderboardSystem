using HexTecGames.Basics.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HexTecGames.LeaderboardSystem
{
    public class ScoreDisplayController : DisplayController<LeaderboardItem>
    {        
        void Awake()
        {
            foreach (var display in displays)
            {
                display.SetItem(null);
            }
        }

        public void UpdatePlayerName(string playerName)
        {
            foreach (var display in displays)
            {
                if (display.IsHighlighted && display is ScoreDisplay scoreDisplay)
                {
                    scoreDisplay.UpdateName(playerName);
                }
            }
        }
        public override void SetItems(List<LeaderboardItem> items, bool display = true)
        {
            base.SetItems(items, display);          
            gameObject.SetActive(true);
        }
    }
}