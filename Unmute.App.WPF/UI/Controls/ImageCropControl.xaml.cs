using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Unmute.App.WPF.UI.Controls
{
    public partial class ImageCropControl : UserControl, INotifyPropertyChanged
    {
        private float left, right, top, bottom;

        public ImageSource? PreviewImage
        {
            get => field;
            set
            {
                field = value;
                this.OnPropertyChanged();
            }
        }

        public RectangleF CropArea => new(left, top, right - left, bottom - top);

        public ImageCropControl()
        {
            right = 1f;
            bottom = 1f;
            InitializeComponent();
            RegisterThumbHandlers();
            AddDimmedRegion();
        }

        private void AddDimmedRegion()
        {
            var columns = this.SelectionContainer.ColumnDefinitions;
            var rows = this.SelectionContainer.RowDefinitions;

            var gridChildren = new HashSet<(int,int)>();
            foreach (UIElement child in this.SelectionContainer.Children)
            {
                gridChildren.Add((Grid.GetColumn(child), Grid.GetRow(child)));
            }

            var brush = new SolidColorBrush(Colors.Black)
            {
                Opacity = 0.75f
            };
            for (var i = 0; i < columns.Count; i++)
            {
                for (var j = 0; j < rows.Count; j++)
                {
                    if (gridChildren.Contains((i, j)))
                        continue;

                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Fill = brush
                    };
                    Grid.SetColumn(rect, i);
                    Grid.SetRow(rect, j);
                    this.SelectionContainer.Children.Add(rect);
                }
            }
        }

        private void OnPreviewChanged(object sender, EventArgs e)
        {
            this.RepositionSelectionRect();
        }

        private void RepositionSelectionRect()
        {
            this.CropControl.Width = this.PreviewControl?.ActualWidth ?? 0;
            this.CropControl.Height = this.PreviewControl?.ActualHeight ?? 0;
            this.SelectionContainer.Width = this.CropControl.Width;
            this.SelectionContainer.Height = this.CropControl.Height;

            this.Space_Left.Width = new GridLength(this.CropControl.Width * left);
            this.Space_Top.Height = new GridLength(this.CropControl.Height * top);
            this.Space_Right.Width = new GridLength(this.CropControl.Width * (1 - right));
            this.Space_Bottom.Height = new GridLength(this.CropControl.Height * (1 - bottom));
        }

        #region dragging        
        private System.Windows.Point dragStartPoint;
        private bool isDragging;

        private void SelectionRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            dragStartPoint = e.GetPosition(this.CropControl);
            SelectionRect.CaptureMouse();
        }

        private void SelectionRect_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging)
                return;

            var current = e.GetPosition(this.CropControl);
            var dx = (float) ((current.X - dragStartPoint.X) / this.CropControl.Width);
            var dy = (float) ((current.Y - dragStartPoint.Y) / this.CropControl.Height);

            // Shift percentages
            this.left += dx;
            this.right += dx;
            this.top += dy;
            this.bottom += dy;

            // Bound to min/max percentages
            if(this.left < 0)
            {
                this.right -= this.left;
                this.left = 0;
            }
            if (this.right > 1)
            {
                this.left -= this.right - 1;
                this.right = 1;
            }
            if (this.top < 0)
            {
                this.bottom -= this.top;
                this.top = 0;
            }
            if (this.bottom > 1)
            {
                this.top -= this.bottom - 1;
                this.bottom = 1;
            }

            dragStartPoint = current;

            this.RepositionSelectionRect();
        }

        private void SelectionRect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            this.SelectionRect.ReleaseMouseCapture();

            this.RepositionSelectionRect();
        }
        #endregion

        #region resize
        private void RegisterThumbHandlers()
        {
            this.ThumbNW.DragDelta += (_, delta) =>
            {
                var dx = delta.HorizontalChange / this.CropControl.Width;
                this.left += (float)dx;
                this.left = Math.Max(this.left, 0);
                this.left = Math.Min(this.left, this.right);

                var dy = delta.VerticalChange / this.CropControl.Height;
                this.top += (float)dy;
                this.top = Math.Max(this.top, 0);
                this.top = Math.Min(this.top, this.bottom);

                this.RepositionSelectionRect();
            };

            this.ThumbNE.DragDelta += (_, delta) =>
            {
                var dx = delta.HorizontalChange / this.CropControl.Width;
                this.right += (float)dx;
                this.right = Math.Max(this.right, left);
                this.right = Math.Min(this.right, 1);

                var dy = delta.VerticalChange / this.CropControl.Height;
                this.top += (float)dy;
                this.top = Math.Max(this.top, 0);
                this.top = Math.Min(this.top, this.bottom);

                this.RepositionSelectionRect();
            };

            this.ThumbSW.DragDelta += (_, delta) =>
            {
                var dx = delta.HorizontalChange / this.CropControl.Width;
                this.left += (float)dx;
                this.left = Math.Max(this.left, 0);
                this.left = Math.Min(this.left, this.right);

                var dy = delta.VerticalChange / this.CropControl.Height;
                this.bottom += (float)dy;
                this.bottom = Math.Max(this.bottom, top);
                this.bottom = Math.Min(this.bottom, 1);

                this.RepositionSelectionRect();
            };

            this.ThumbSE.DragDelta += (_, delta) =>
            {
                var dx = delta.HorizontalChange / this.CropControl.Width;
                this.right += (float)dx;
                this.right = Math.Max(this.right, left);
                this.right = Math.Min(this.right, 1);

                var dy = delta.VerticalChange / this.CropControl.Height;
                this.bottom += (float)dy;
                this.bottom = Math.Max(this.bottom, top);
                this.bottom = Math.Min(this.bottom, 1);

                this.RepositionSelectionRect();
            };
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
