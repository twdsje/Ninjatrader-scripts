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
	public class BigCandle : Indicator
	{
		private double priorSum;
		private double sum;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Indicator to help highlight candles that have an unusualy high range.";
				Name										= "BigCandle";
				Calculate									= Calculate.OnPriceChange;
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
				DeviationFactor					= 3;
				Period = 120;
				AlertEnabled = false;
				AlertPriority = Priority.Medium;
				AlertRearm = 300;
				DrawArrows = true;
				Highlight = Brushes.Aquamarine;
				AddPlot(new Stroke(Brushes.DimGray, 2), PlotStyle.Bar, "Range");
				AddPlot(Brushes.MidnightBlue, "AverageRange");
				AddPlot(Brushes.SlateBlue, "RangeDeviation");
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			Range[0] = High[0] - Low[0];
			
			AverageRange[0] = SMA(Range, Period)[0];
			double currentDeviation = StdDev(Range, Period)[0];
			
			RangeDeviation[0] = AverageRange[0] + currentDeviation * DeviationFactor;
			
			if(Range[0] > RangeDeviation[0])
			{
				PlotBrushes[0][0] = Highlight;
				
				if(DrawArrows)
					Draw.ArrowUp(this, "BigCandle " + Time[0].ToString(), true, 0, Low[0], Highlight);
				
				if(AlertEnabled)
					Alert("BigCandle", AlertPriority, "Unusual price movement",  NinjaTrader.Core.Globals.InstallDir+@"\sounds\Alert1.wav", AlertRearm, Brushes.Black, Highlight);
			}
			
		}

		#region Properties
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Deviation Factor", Description="Number of standard deviations required to highlight a bar as unusual.", Order=1, GroupName="Parameters")]
		public int DeviationFactor
		{ get; set; }
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name = "Period", GroupName = "Parameters", Order = 1)]
		public int Period
		{ get; set; }

		[XmlIgnore()] 
		[Display(Name = "Highlight Color", GroupName = "Parameters", Order = 3)]
		public SolidColorBrush Highlight
		{ get; set; }
		
		[Browsable(false)]
		public string HighlightSerialize
		{
			get { return Serialize.BrushToString(Highlight); }
			set { Highlight = (SolidColorBrush)Serialize.StringToBrush(value); }
		}

		[Display(Name = "Show Markers", GroupName = "Parameters", Order = 4)]
		public bool DrawArrows
		{ get; set;}
		
		[Display(Name = "Enable Alerts", GroupName = "Alert", Order = 0)]
		public bool AlertEnabled
		{ get; set;}

		[Display(Name = "Priority", GroupName = "Alert", Order = 0)]
		public Priority AlertPriority
		{ get; set;}
		
		[Range(1, int.MaxValue)]
		[Display(Name="Alert Rearm Time", Description="How long before this alert can be triggered again.", Order=1, GroupName="Alert")]
		public int AlertRearm
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Range
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> AverageRange
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeDeviation
		{
			get { return Values[2]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BigCandle[] cacheBigCandle;
		public BigCandle BigCandle(int deviationFactor, int period)
		{
			return BigCandle(Input, deviationFactor, period);
		}

		public BigCandle BigCandle(ISeries<double> input, int deviationFactor, int period)
		{
			if (cacheBigCandle != null)
				for (int idx = 0; idx < cacheBigCandle.Length; idx++)
					if (cacheBigCandle[idx] != null && cacheBigCandle[idx].DeviationFactor == deviationFactor && cacheBigCandle[idx].Period == period && cacheBigCandle[idx].EqualsInput(input))
						return cacheBigCandle[idx];
			return CacheIndicator<BigCandle>(new BigCandle(){ DeviationFactor = deviationFactor, Period = period }, input, ref cacheBigCandle);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BigCandle BigCandle(int deviationFactor, int period)
		{
			return indicator.BigCandle(Input, deviationFactor, period);
		}

		public Indicators.BigCandle BigCandle(ISeries<double> input , int deviationFactor, int period)
		{
			return indicator.BigCandle(input, deviationFactor, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BigCandle BigCandle(int deviationFactor, int period)
		{
			return indicator.BigCandle(Input, deviationFactor, period);
		}

		public Indicators.BigCandle BigCandle(ISeries<double> input , int deviationFactor, int period)
		{
			return indicator.BigCandle(input, deviationFactor, period);
		}
	}
}

#endregion
