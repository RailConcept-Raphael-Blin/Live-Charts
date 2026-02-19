using LiveCharts.Helpers;
using System.Collections.Generic;

namespace LiveCharts.Wpf
{
    /// <summary>
    /// 
    /// </summary>
    public class AxisWindowCollection : NoisyCollection<AxisWindow>
    {
        public AxisWindowCollection()
        {
            NoisyCollectionChanged += OnNoisyCollectionChanged;
        }

        private void OnNoisyCollectionChanged(IEnumerable<AxisWindow> oldItems, IEnumerable<AxisWindow> newItems)
        {

        }
    }
}
