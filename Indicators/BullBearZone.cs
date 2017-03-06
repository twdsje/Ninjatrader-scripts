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
	public class BullBearZone : Indicator
	{
		private SessionIterator sessionIterator;
		private int SessionNumber = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"50% retracement between previous regular session close and overnight extreme.";
				Name										= "BullBearZone";
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
				
				BarsRequiredToPlot = 3;
				ZoneColor = Brushes.Blue;
			}
			else if (State == State.Configure)
			{
				sessionIterator = new SessionIterator(Bars);
				//AddDataSeries(BarsPeriodType.Day, 1);
			}
		}

		protected override void OnBarUpdate()
		{
			//if (Bars < BarsRequiredToPlot) return;
			
			if(Bars.IsFirstBarOfSession)
			{
				sessionIterator.GetNextSession(Time[0], true);
				SessionNumber++;
				
				if(SessionNumber < 3) return;
				
				double previousClose = PriorDayOHLC().PriorClose[1];
				double overnightHigh = PriorDayOHLC().PriorHigh[0];
				double overnightLow = PriorDayOHLC().PriorLow[0];
				
				double bullBearCenterpoint = 0;
				double bullBearCenterpoint2 = 0;
				
				double highDiff = Math.Abs(previousClose - overnightHigh);
				double lowDiff = Math.Abs(previousClose - overnightLow);
				
				if(highDiff > lowDiff)
				{
					bullBearCenterpoint = previousClose + (highDiff / 2);
				}
				else if (highDiff < lowDiff)
				{
					bullBearCenterpoint = previousClose - (lowDiff / 2);
				}
				else
				{
					bullBearCenterpoint = previousClose + (highDiff / 2);
					bullBearCenterpoint2 = previousClose - (lowDiff / 2);
				}
				
				Draw.Rectangle(this, "Bull Bear Zone" + Time[0].ToString(), Time[0], bullBearCenterpoint - (TickSize * 2), sessionIterator.ActualSessionEnd, bullBearCenterpoint + (TickSize * 2), ZoneColor);
				
				if(bullBearCenterpoint2 != 0)
				{
					Draw.Rectangle(this, "Bull Bear Zone" + Time[0].ToString(), Time[0], bullBearCenterpoint2 - (TickSize * 2), sessionIterator.ActualSessionEnd, bullBearCenterpoint2 + (TickSize * 2), ZoneColor);
				}
			}		
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Zone Color", GroupName = "Parameters", Order = 1)]
		public Brush ZoneColor
		{ get; set; }
		
		[Browsable(false)]
		public string ZoneColorSerializable
		{
			get { return Serialize.BrushToString(ZoneColor); }
			set { ZoneColor = Serialize.StringToBrush(value); }
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BullBearZone[] cacheBullBearZone;
		public BullBearZone BullBearZone(Brush zoneColor)
		{
			return BullBearZone(Input, zoneColor);
		}

		public BullBearZone BullBearZone(ISeries<double> input, Brush zoneColor)
		{
			if (cacheBullBearZone != null)
				for (int idx = 0; idx < cacheBullBearZone.Length; idx++)
					if (cacheBullBearZone[idx] != null && cacheBullBearZone[idx].ZoneColor == zoneColor && cacheBullBearZone[idx].EqualsInput(input))
						return cacheBullBearZone[idx];
			return CacheIndicator<BullBearZone>(new BullBearZone(){ ZoneColor = zoneColor }, input, ref cacheBullBearZone);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BullBearZone BullBearZone(Brush zoneColor)
		{
			return indicator.BullBearZone(Input, zoneColor);
		}

		public Indicators.BullBearZone BullBearZone(ISeries<double> input , Brush zoneColor)
		{
			return indicator.BullBearZone(input, zoneColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BullBearZone BullBearZone(Brush zoneColor)
		{
			return indicator.BullBearZone(Input, zoneColor);
		}

		public Indicators.BullBearZone BullBearZone(ISeries<double> input , Brush zoneColor)
		{
			return indicator.BullBearZone(input, zoneColor);
		}
	}
}

#endregion
