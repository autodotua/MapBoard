using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapBoard.UI.Util
{
    public static class VisualUtillity
    {
        public static T GetVisualChild<T>(this Visual referenceVisual) where T : Visual
        {
            Visual child = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(referenceVisual); i++)
            {
                child = VisualTreeHelper.GetChild(referenceVisual, i) as Visual;
                if (child != null && child is T)
                {
                    break;
                }
                else if (child != null)
                {
                    child = GetVisualChild<T>(child);
                    if (child != null && child is T)
                    {
                        break;
                    }
                }
            }
            return child as T;
        }

        public static List<T> GetVisualChilds<T>(this Visual referenceVisual) where T : Visual
        {
            List<T> results = new List<T>();
            Get(referenceVisual);
            void Get(Visual visual)
            {
                Visual child = null;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
                {
                    child = VisualTreeHelper.GetChild(visual, i) as Visual;
                    if (child != null && child is T t)
                    {
                        results.Add(t);
                    }
                    if (child != null)
                    {
                        Get(child);
                    }
                }
            }
            return results;
        }
    }
}