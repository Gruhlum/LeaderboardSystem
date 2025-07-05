using HexTecGames.Basics.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexTecGames.LeaderboardSystem
{
	public class ScoreDisplay : Display<ScoreDisplay, LeaderboardItem>
	{
        [SerializeField] private TMP_Text rankGUI = default;
        [SerializeField] private TMP_Text nameGUI = default;
        [SerializeField] private TMP_Text scoreGUI = default;
        [SerializeField] private Image background = default;

        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightedColor = Color.white;

        protected override void DrawItem(LeaderboardItem item)
        {
            if (item == null)
            {
                rankGUI.text = null;
                nameGUI.text = null;
                scoreGUI.text = null;
                SetHighlight(false);
                return;
            }
            rankGUI.text = item.rank.ToString();
            UpdateName(item.name);
            scoreGUI.text = item.score.ToString();
            SetHighlight(item.isHighlighted);
        }
        public void UpdateName(string name)
        {
            if (Item == null)
            {
                return;
            }
            Item.name = name;
            nameGUI.text = name;
        }
        public override void SetHighlight(bool active)
        {
            background.color = active ? highlightedColor : normalColor;
            base.SetHighlight(active);
        }
    }
}