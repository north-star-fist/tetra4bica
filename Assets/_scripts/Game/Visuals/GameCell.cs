using System;
using System.Collections.Generic;
using Tetra4bica.Core;
using UnityEngine;

namespace Tetra4bica.Graphics
{

    public class GameCell : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer spriteRenderer;
        [SerializeField]
        private ColorSprite[] spriteMap;

        public SpriteRenderer SpriteRenderer => spriteRenderer;

        public CellColor? CellColor => cellColor;
        private CellColor? cellColor;

        private readonly Dictionary<CellColor, Sprite> colorToSpriteMap = new Dictionary<CellColor, Sprite>();

        private void Awake()
        {
            if (spriteMap != null)
            {
                foreach (var pair in spriteMap)
                {
                    colorToSpriteMap.Add(pair.Color, pair.Sprite);
                }
            }
        }

        public void SetColor(CellColor? cellColor)
        {
            this.cellColor = cellColor;

            paintCell(spriteRenderer, cellColor);
        }

        private void paintCell(SpriteRenderer spriteRenderer, CellColor? color)
        {
            if (color.HasValue)
            {
                spriteRenderer.sprite = colorToSpriteMap[color.Value];
                //spriteRenderer.color = Cells.ToUnityColor(color.Value);
            }
            spriteRenderer.enabled = color.HasValue;
        }


        [Serializable]
        public class ColorSprite
        {
            public CellColor Color;
            public Sprite Sprite;
        }
    }
}
