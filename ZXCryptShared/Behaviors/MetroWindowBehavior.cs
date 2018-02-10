using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

using MahApps.Metro.Controls;

namespace ZXCryptShared
{
    /// <summary>
    /// Blend Behaviors for metro window
    /// </summary>
    public class MetroWindowBehavior : Behavior<MetroWindow>
    {
        /// <summary>
        /// Close window behavior
        /// </summary>
        public bool CloseWindow
        {
            get { return (bool)GetValue(CloseWindowProperty); }
            set { SetValue(CloseWindowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CloseWindow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CloseWindowProperty =
            DependencyProperty.Register("CloseWindow", typeof(bool), typeof(MetroWindowBehavior), new PropertyMetadata(false, OnCloseWindowChanged));

        private static void OnCloseWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (MetroWindowBehavior)d;
            if (behavior != null)
            {
                behavior.OnCloseWindowChanged(e);
            }
        }

        private void OnCloseWindowChanged(DependencyPropertyChangedEventArgs e)
        {
            if (this.CloseWindow)
            {
                this.AssociatedObject.Close();
            }
        }
    }
}
