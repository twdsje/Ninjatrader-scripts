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
	public class ZipperLength : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"How long has it been in a range for?";
				Name										= "ZipperLength";
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
				
				Width					= 2;
				
				AddPlot(new Stroke(Brushes.Goldenrod, 2), PlotStyle.Bar, "ZipperLength");
			}
			else if (State == State.Configure)
			{
				
			}
		}

		protected override void OnBarUpdate()
		{			
			if (CurrentBar < Width)
			{
				Value[0] = 0;
        		return;
			}
			
			int current = 0;
			double high = High[current];
			double low = Low[current];
			
			do
			{
				high = Math.Max(high, High[current]);
				low = Math.Min(low, Low[current]);
				current++;
			}
			while(high - low <= Width * TickSize && current < CurrentBar);
			
			Value[0] = current;
			
			//Colors
			if(current > Width * 3.5)
			{
				PlotBrushes[0][0] = Brushes.Orange;
			}	
			else if(current > Width * 1.5)
			{
				PlotBrushes[0][0] = Brushes.CornflowerBlue;
			}
			else if(current > Width)
			{
				PlotBrushes[0][0] = Brushes.DarkGray;
			}
			else
			{
				PlotBrushes[0][0] = Brushes.Black;
			}			
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name="Width", Order=1, GroupName="Parameters")]
		public int Width
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ZipperLength[] cacheZipperLength;
		public ZipperLength ZipperLength(int width)
		{
			return ZipperLength(Input, width);
		}

		public ZipperLength ZipperLength(ISeries<double> input, int width)
		{
			if (cacheZipperLength != null)
				for (int idx = 0; idx < cacheZipperLength.Length; idx++)
					if (cacheZipperLength[idx] != null && cacheZipperLength[idx].Width == width && cacheZipperLength[idx].EqualsInput(input))
						return cacheZipperLength[idx];
			return CacheIndicator<ZipperLength>(new ZipperLength(){ Width = width }, input, ref cacheZipperLength);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ZipperLength ZipperLength(int width)
		{
			return indicator.ZipperLength(Input, width);
		}

		public Indicators.ZipperLength ZipperLength(ISeries<double> input , int width)
		{
			return indicator.ZipperLength(input, width);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ZipperLength ZipperLength(int width)
		{
			return indicator.ZipperLength(Input, width);
		}

		public Indicators.ZipperLength ZipperLength(ISeries<double> input , int width)
		{
			return indicator.ZipperLength(input, width);
		}
	}
}

#endregion
