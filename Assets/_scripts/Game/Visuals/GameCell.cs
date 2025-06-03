using System;
using System.Collections.Generic;
using Tetra4bica.Core;
using UnityEngine;

namespace Tetra4bica.Graphics
{

    public class GameCell : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _spriteRenderer;
        [SerializeField]
        private ColorSprite[] _spriteMap;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        public CellColor? CellColor => _cellColor;
        private CellColor? _cellColor;

        private readonly Dictionary<CellColor, Sprite> _colorToSpriteMap = new Dictionary<CellColor, Sprite>();

        private void Awake()
        {
            if (_spriteMap != null)
            {
                foreach (var pair in _spriteMap)
                {
                    _colorToSpriteMap.Add(pair.Color, pair.Sprite);
                }
            }
        }

        public void SetColor(CellColor? cellColor)
        {
            this._cellColor = cellColor;

            paintCell(_spriteRenderer, cellColor);
        }

        private void paintCell(SpriteRenderer spriteRenderer, CellColor? color)
        {
            if (color.HasValue)
            {
                spriteRenderer.sprite = _colorToSpriteMap[color.Value];
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
