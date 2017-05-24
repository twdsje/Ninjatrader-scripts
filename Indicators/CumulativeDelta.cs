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

public enum CumulativeDeltaTimeframe { Session, Week, Month, Never };

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	
	
	public class CumulativeDelta : Indicator
	{			
		public class Profile
		{
			public long CurrentCumulativeDelta;

			public Profile()
			{	
				CurrentCumulativeDelta = 0;
			}
			
			public void AddAskVolume(double price, long volume)
			{
				CurrentCumulativeDelta += volume;
			}
			
			public void AddBidVolume(double price, long volume)
			{
				CurrentCumulativeDelta -= volume;
			}
		}		
		
		private Profile myProfile = new Profile();
		private SessionIterator sessionIterator;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "CumulativeDelta";
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
				AddPlot(Brushes.CornflowerBlue, "CumDelta");
				
				ResetDeltaOn = CumulativeDeltaTimeframe.Never;
			}
			else if (State == State.Configure)
			{
				sessionIterator = new SessionIterator(Bars);
			}
		}

		protected override void OnBarUpdate()
		{
			
			if(CurrentBars[0] <= BarsRequiredToPlot) return;

			//Reset profile on session week or day.
			if(IsFirstTickOfBar && ResetDeltaOn != CumulativeDeltaTimeframe.Never)
			{
				DateTime previous = sessionIterator.GetTradingDay(Time[1]);
				DateTime current = sessionIterator.GetTradingDay(Time[0]);
				
				//Reset profile on daily basis.
				if(ResetDeltaOn == CumulativeDeltaTimeframe.Session && !current.DayOfWeek.Equals(previous.DayOfWeek))
				{				
					myProfile = new Profile();
				}
				
				//Reset profile on weekly basis.
				else if(ResetDeltaOn == CumulativeDeltaTimeframe.Week && current.DayOfWeek.CompareTo(previous.DayOfWeek) < 0)
				{
					myProfile = new Profile();
				}
				
				//Reset profile on monthly basis.
				else if(ResetDeltaOn != CumulativeDeltaTimeframe.Month && !current.Month.Equals(previous.Month))
				{
					myProfile = new Profile();
				}
			}
			
			Value[0] = myProfile.CurrentCumulativeDelta;
		}
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last)
			{
				if (e.Price >= e.Ask)
				{		
					myProfile.AddAskVolume(e.Price, e.Volume);
				}
				else if(e.Price <= e.Bid)
				{
					myProfile.AddBidVolume(e.Price, e.Volume);
				}
			}
		}

		#region Properties

		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name="ResetDeltaOn", Description="Reset Profile On", Order=0, GroupName="General")]
		public CumulativeDeltaTimeframe ResetDeltaOn
		{ get; set; }
		
		[XmlIgnore]
		[Browsable(false)]
		public string ResetDeltaOnSerializable
		{
			get { return ResetDeltaOn.ToString(); }
			set { ResetDeltaOn = (CumulativeDeltaTimeframe) Enum.Parse(typeof(CumulativeDeltaTimeframe), value); }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CumDelta
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
		private CumulativeDelta[] cacheCumulativeDelta;
		public CumulativeDelta CumulativeDelta(CumulativeDeltaTimeframe resetDeltaOn)
		{
			return CumulativeDelta(Input, resetDeltaOn);
		}

		public CumulativeDelta CumulativeDelta(ISeries<double> input, CumulativeDeltaTimeframe resetDeltaOn)
		{
			if (cacheCumulativeDelta != null)
				for (int idx = 0; idx < cacheCumulativeDelta.Length; idx++)
					if (cacheCumulativeDelta[idx] != null && cacheCumulativeDelta[idx].ResetDeltaOn == resetDeltaOn && cacheCumulativeDelta[idx].EqualsInput(input))
						return cacheCumulativeDelta[idx];
			return CacheIndicator<CumulativeDelta>(new CumulativeDelta(){ ResetDeltaOn = resetDeltaOn }, input, ref cacheCumulativeDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CumulativeDelta CumulativeDelta(CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.CumulativeDelta(Input, resetDeltaOn);
		}

		public Indicators.CumulativeDelta CumulativeDelta(ISeries<double> input , CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.CumulativeDelta(input, resetDeltaOn);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CumulativeDelta CumulativeDelta(CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.CumulativeDelta(Input, resetDeltaOn);
		}

		public Indicators.CumulativeDelta CumulativeDelta(ISeries<double> input , CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.CumulativeDelta(input, resetDeltaOn);
		}
	}
}

#endregion
