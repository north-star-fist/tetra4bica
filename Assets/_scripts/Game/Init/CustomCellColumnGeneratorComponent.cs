using System;
using Tetra4bica.Core;
using UnityEngine;

namespace Tetra4bica.Init
{
    public class CustomCellColumnGeneratorComponent : MonoBehaviour, ICellGenerator
    {

        virtual public void GenerateCells(CellColor[] arrayToFill)
        {
            throw new NotImplementedException();
        }
    }
}
