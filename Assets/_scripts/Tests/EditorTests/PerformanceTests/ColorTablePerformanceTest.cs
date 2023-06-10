using NUnit.Framework;
using System;
using Tetra4bica.Core;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Tetra4bica.Tests.Performance {
    internal class ColorTablePerformanceTest {

        Vector2Int[] matchedCellsBuffer = new Vector2Int[16];

        [Test, Performance, Version("2")]
        public void TestFlickeringCell() {
            ColorTable table = new ColorTable(5, 5);
            for (int i = 0; i < 5; i++) {
                table.ScrollLeft(new CellColor[] { CellColor.Green, CellColor.Green, CellColor.Green, CellColor.Green, CellColor.Green });
            }

            Measure.Method(() => {
                table[2, 2] = CellColor.NONE;
                table[2, 2] = CellColor.Red;
                table[2, 2] = CellColor.NONE;
                table[2, 2] = CellColor.Green;
            }).GC().Run();
        }

        [Test, Performance, Version("2")]
        public void TestRegionSwappingCell() {
            ColorTable table = new ColorTable(5, 5);
            table.ScrollLeft(new CellColor[] { CellColor.Red, CellColor.Red, CellColor.Red, CellColor.Red, CellColor.Red });
            table.ScrollLeft(new CellColor[] { CellColor.Green, CellColor.Red, CellColor.Red, CellColor.Red, CellColor.Green });
            table.ScrollLeft(new CellColor[] { CellColor.Green, CellColor.Green, CellColor.Green, CellColor.Green, CellColor.Green });
            table.ScrollLeft(new CellColor[] { CellColor.Green, CellColor.Red, CellColor.Red, CellColor.Red, CellColor.Green });
            table.ScrollLeft(new CellColor[] { CellColor.Red, CellColor.Red, CellColor.Red, CellColor.Red, CellColor.Red });

            Measure.Method(() => {
                table[2, 2] = CellColor.NONE;
                table[2, 2] = CellColor.Green;
                table[2, 2] = CellColor.NONE;
                table[2, 2] = CellColor.Red;
            }).GC().Run();
        }

        [Test, Performance, Version("1")]
        public void TestScrollLeft() {
            ColorTable table = new ColorTable(5, 5);
            CellColor[] wall1 = new CellColor[] { CellColor.Yellow, CellColor.Green, CellColor.NONE, CellColor.Yellow, CellColor.Orange };
            CellColor[] wall2 = new CellColor[] { CellColor.Red, CellColor.PaleBlue, CellColor.Blue, CellColor.Magenta, CellColor.Orange };
            CellColor[] wall3 = new CellColor[] { CellColor.NONE, CellColor.Green, CellColor.NONE, CellColor.NONE, CellColor.PaleBlue };
            CellColor[] wall4 = new CellColor[] { CellColor.Magenta, CellColor.Green, CellColor.Blue, CellColor.Yellow, CellColor.Orange };
            CellColor[] wall5 = new CellColor[] { CellColor.Red, CellColor.NONE, CellColor.Blue, CellColor.Yellow, CellColor.Orange };
            Measure.Method(() => {
                table.ScrollLeft(wall1);
                table.ScrollLeft(wall2);
                table.ScrollLeft(wall3);
                table.ScrollLeft(wall4);
                table.ScrollLeft(wall5);
            }).GC().Run();
        }
    }
}