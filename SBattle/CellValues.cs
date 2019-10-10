using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SBattle.UI;
using System.Windows.Media;

namespace SBattle
{
    /// <summary>
    /// Класс значений ячеек игрового поля
    /// </summary>
    static class CellValues
    {
        public static readonly BattleFieldColorsCollection BattleFieldColors = new BattleFieldColorsCollection(new[]{
            Brushes.SkyBlue,
            Brushes.Green,
            Brushes.Red,
            Brushes.DarkRed,
            Brushes.LightYellow
        });

        public const int None = 0;
        public const int Own = 1;
        public const int Dead = 2;
        public const int CompletlyDead = 3;
        public const int FiredEmpty = 4;
    }

    /// <summary>
    /// Класс значений границ игрового поля и сетки
    /// </summary>
    static class BorderValues
    {
        public static readonly BattleFieldColorsCollection BattleFieldBorderColors = new BattleFieldColorsCollection(new[]{
            Brushes.Transparent,
            Brushes.Green,
            Brushes.Salmon
        });

        public const int None = 0;
        public const int PlaceSelection = 1;
        public const int TargetSelection = 2;
    }
}
