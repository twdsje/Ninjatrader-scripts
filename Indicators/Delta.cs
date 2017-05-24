#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class Delta : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Shows change in price.";
				Name										= "Delta";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Bar, "DeltaPlot");
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
					Value[0] = Input[0];
			else
			{
				Value[0] = Input[0] - Input [1];
			}
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DeltaPlot
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Delta[] cacheDelta;
		public Delta Delta()
		{
			return Delta(Input);
		}

		public Delta Delta(ISeries<double> input)
		{
			if (cacheDelta != null)
				for (int idx = 0; idx < cacheDelta.Length; idx++)
					if (cacheDelta[idx] != null &&  cacheDelta[idx].EqualsInput(input))
						return cacheDelta[idx];
			return CacheIndicator<Delta>(new Delta(), input, ref cacheDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Delta Delta()
		{
			return indicator.Delta(Input);
		}

		public Indicators.Delta Delta(ISeries<double> input )
		{
			return indicator.Delta(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Delta Delta()
		{
			return indicator.Delta(Input);
		}

		public Indicators.Delta Delta(ISeries<double> input )
		{
			return indicator.Delta(input);
		}
	}
}

#endregion
