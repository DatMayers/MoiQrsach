using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SBattle.UI
{
    /// <summary>
    /// Логика игрового поля BattleField.xaml
    /// </summary>
    public partial class BattleField : Grid
    {
        class BattleFieldCell : IBattleFieldCell
        {
            private int _value;
            private int _borderValue;

            public int Value
            {
                get { return _value; }
                set
                {
                    _control.Background = _owner.Colors[value];
                    _value = value;
                }
            }

            public int BorderValue
            {
                get { return _value; }
                set
                {
                    _control.BorderBrush = _owner.BorderColors[value];
                    _borderValue = value;
                }
            }

            public int X { get; private set; }
            public int Y { get; private set; }

            BattleField _owner;
            UserControl _control;

            public UserControl Control { get { return _control; } }

            public BattleFieldCell(BattleField owner, int x, int y)
            {
                _owner = owner;

                this.X = x;
                this.Y = y;

                _control = new UserControl() {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = owner.Colors[0],
                    BorderThickness = new Thickness(1.0),
                    BorderBrush = Brushes.Transparent
                };
            }
        }

        #region int FieldSize

        public int FieldSize
        {
            get { return (int)GetValue(FieldSizeProperty); }
            set { SetValue(FieldSizeProperty, value); }
        }

        public static readonly DependencyProperty FieldSizeProperty =
            DependencyProperty.Register("FieldSize", typeof(int), typeof(BattleField), new UIPropertyMetadata(0));

        #endregion

        #region BattleFieldColorsCollection Colors

        public BattleFieldColorsCollection Colors
        {
            get { return (BattleFieldColorsCollection)GetValue(ColorsProperty); }
            set { SetValue(ColorsProperty, value); }
        }

        public static readonly DependencyProperty ColorsProperty =
            DependencyProperty.Register("Colors", typeof(BattleFieldColorsCollection), typeof(BattleField), new UIPropertyMetadata(null));

        #endregion

        #region BattleFieldColorsCollection BorderColors

        public BattleFieldColorsCollection BorderColors
        {
            get { return (BattleFieldColorsCollection)GetValue(BorderColorsProperty); }
            set { SetValue(BorderColorsProperty, value); }
        }

        public static readonly DependencyProperty BorderColorsProperty =
            DependencyProperty.Register("BorderColors", typeof(BattleFieldColorsCollection), typeof(BattleField), new UIPropertyMetadata(null));

        #endregion

        public event EventHandler<BattleFieldCellEventArgs> OnBattleFieldCellMouseUp = delegate { };
        public event EventHandler<BattleFieldCellEventArgs> OnBattleFieldCellMouseEnter = delegate { };
        public event EventHandler<BattleFieldCellEventArgs> OnBattleFieldCellMouseLeave = delegate { };

        private BattleFieldCell[,] _cells;

        public IBattleFieldCell this[int x, int y]
        {
            get { return _cells[x, y]; }
        }

        /// <summary>
        /// Конструктор класса игрового поля
        /// </summary>
        public BattleField()
        {
            InitializeComponent();
            this.Colors = new BattleFieldColorsCollection(new[] { Brushes.SkyBlue });
            this.BorderColors = new BattleFieldColorsCollection(new[] { Brushes.Transparent });
            this.FieldSize = 10;
        }

        private void ReBuildContents()
        {
            contents.Children.Clear();
            contents.RowDefinitions.Clear();
            contents.ColumnDefinitions.Clear();

            int sz = this.FieldSize;

            _cells = new BattleFieldCell[sz, sz];

            contents.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            contents.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            for (int i = 0; i < sz; i++)
            {
                contents.RowDefinitions.Add(new RowDefinition());
                contents.ColumnDefinitions.Add(new ColumnDefinition());

                var hh = new UserControl() {
                    Content = ((char)('A' + i)).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                contents.Children.Add(hh);
                Grid.SetRow(hh, 0);
                Grid.SetColumn(hh, i + 1);

                var vh = new UserControl() {
                    Content = (i + 1).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                contents.Children.Add(vh);
                Grid.SetRow(vh, i + 1);
                Grid.SetColumn(vh, 0);
            }

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    var cell = new BattleFieldCell(this, x, y);
                    _cells[x, y] = cell;
                    contents.Children.Add(cell.Control);
                    Grid.SetRow(cell.Control, y + 1);
                    Grid.SetColumn(cell.Control, x + 1);
                    cell.Control.PreviewMouseDown += (sender, ea) => ea.Handled = true;
                    cell.Control.MouseUp += (sender, ea) => this.OnBattleFieldCellMouseUp(this, new BattleFieldCellEventArgs(cell, ea));
                    cell.Control.MouseEnter += (sender, ea) => this.OnBattleFieldCellMouseEnter(this, new BattleFieldCellEventArgs(cell, ea));
                    cell.Control.MouseLeave += (sender, ea) => this.OnBattleFieldCellMouseLeave(this, new BattleFieldCellEventArgs(cell, ea));
                }
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ColorsProperty ||
                e.Property == BorderColorsProperty ||
                e.Property == FieldSizeProperty)
            {
                this.ReBuildContents();
            }

            base.OnPropertyChanged(e);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var r = Math.Min(arrangeSize.Width, arrangeSize.Height);
            arrangeSize = new Size(r, r);
            var rv = base.ArrangeOverride(arrangeSize);
            return rv;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var rv = base.MeasureOverride(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (constraint.Width == double.PositiveInfinity || constraint.Height == double.PositiveInfinity)
            {
                var c = Math.Max(rv.Width, rv.Height);
                return new Size(c, c);
            }
            else
            {
                var c = Math.Max(Math.Max(Math.Min(constraint.Width, constraint.Height), rv.Height), rv.Width);
                return new Size(c, c);
            }
        }
    }

    /// <summary>
    /// Интерфейс ячейки
    /// </summary>
    public interface IBattleFieldCell
    {
        int Value { get; set; }
        int BorderValue { get; set; }
        int X { get; }
        int Y { get; }
    }

    /// <summary>
    /// Класс обработчика событий 
    /// </summary>
    public class BattleFieldCellEventArgs : EventArgs
    {
        public IBattleFieldCell Cell { get; private set; }
        public MouseEventArgs MouseInfo { get; private set; }

        public MouseButton? Button { get; private set; }

        public BattleFieldCellEventArgs(IBattleFieldCell cell, MouseEventArgs mouseInfo)
        {
            this.Cell = cell;
            this.MouseInfo = mouseInfo;
            this.Button = (mouseInfo is MouseButtonEventArgs) ? (mouseInfo as MouseButtonEventArgs).ChangedButton : (MouseButton?)null;
        }
    }

    public class BattleFieldColorsCollection : List<Brush>
    {
        public BattleFieldColorsCollection() : base() { }
        public BattleFieldColorsCollection(IEnumerable<Brush> collection) : base(collection) { }
        public BattleFieldColorsCollection(int capacity) : base(capacity) { }
    }
}
