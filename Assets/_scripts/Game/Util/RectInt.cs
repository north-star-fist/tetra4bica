using System.Text;
using UnityEngine;

namespace Tetra4bica.Util {
    public readonly struct RectInt {

        public readonly Vector2Int Position;
        public readonly Vector2Int Size;

        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxX;
        public readonly int MaxY;

        public RectInt(Vector2Int position, Vector2Int size) {
            Position = position;
            Size = size;
            MinX = position.x;
            MinY = position.y;
            MaxX = position.x + size.x;
            MaxY = position.y + size.y;
        }

        public bool Contains(Vector2Int point) {
            return (point.x >= Position.x && point.x < Position.x + Size.x)
                && (point.y >= Position.y && point.y < Position.y + Size.y);
        }

        /// <summary> Extends the bounds to make a new one that should INCLUDE the passed point. </summary>
        public RectInt Extend(Vector2Int toInclude) {
            var newPosition = Position;
            var newSize = Size;
            if (toInclude.x < newPosition.x) {
                newSize.x = (newPosition + Size).x - toInclude.x;
                newPosition.x = toInclude.x;
            } else if (toInclude.x >= newPosition.x + Size.x) {
                newSize.x = toInclude.x - newPosition.x + 1;
            }
            if (toInclude.y < newPosition.y) {
                newSize.y = (newPosition + Size).y - toInclude.y;
                newPosition.y = toInclude.y;
            } else if (toInclude.y >= newPosition.y + Size.y) {
                newSize.y = toInclude.y - newPosition.y + 1;
            }

            return new(newPosition, newSize);
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(Position.ToString());
            sb.Append('+');
            sb.Append(Size.ToString());
            sb.Append(']');
            return sb.ToString();
        }
    }

}