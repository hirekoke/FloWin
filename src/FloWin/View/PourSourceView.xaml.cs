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

namespace FloWin.View
{
    /// <summary>
    /// DragControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PourSourceView : System.Windows.Controls.Primitives.Thumb
    {
        ViewModel.PourSourceViewModel _viewModel = null;
        public ViewModel.PourSourceViewModel ViewModel
        {
            get { return _viewModel; }
        }

        public PourSourceView()
        {
            InitializeComponent();
            _viewModel = new ViewModel.PourSourceViewModel();
            this.DataContext = _viewModel;

            this.Loaded += (s, e) =>
            {
                Canvas.SetLeft(this, _viewModel.X);
                Canvas.SetTop(this, _viewModel.Y);
            };
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (_viewModel != null) _viewModel.IsDragging = false;
        }

        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (_viewModel != null) _viewModel.IsDragging = true;
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double nx = Canvas.GetLeft(this) + e.HorizontalChange;
            double ny = Canvas.GetTop(this) + e.VerticalChange;

            if (nx < 0) nx = 0;
            if (ny < 0) ny = 0;

            Canvas parentCanvas = this.Parent as Canvas;
            if (parentCanvas != null && nx + this.ActualWidth > parentCanvas.ActualWidth)
                nx = parentCanvas.ActualWidth - this.ActualWidth;
            if (parentCanvas != null && ny + this.ActualHeight > parentCanvas.ActualHeight)
                ny = parentCanvas.ActualHeight - this.ActualHeight;

            Canvas.SetLeft(this, nx);
            Canvas.SetTop(this, ny);

            _viewModel.MoveTo(nx, ny);
        }
    }
}
