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
	public class SpreadCumulativeDelta : Indicator
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
				Description									= @"Shows the difference in volume between two instruments.";
				Name										= "SpreadCumulativeDelta";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				BarsRequiredToPlot							= 1;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= false;
				FrontLeg					= string.Empty;
				BackLeg					= string.Empty;
				FrontRatio 				= 3;
				BackRatio				= 1;
				AddPlot(Brushes.Orange, "MySpread");
				
				ResetDeltaOn = CumulativeDeltaTimeframe.Never;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(FrontLeg, BarsArray[0].BarsPeriod);
				AddDataSeries(BackLeg, BarsArray[0].BarsPeriod);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] <= BarsRequiredToPlot || CurrentBars[1] <= BarsRequiredToPlot || CurrentBars[2] <= BarsRequiredToPlot)
        		return;

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
			if(BarsInProgress == 1)
			{
				if (e.MarketDataType == MarketDataType.Last)
				{
					if (e.Price >= e.Ask)
					{		
						myProfile.AddAskVolume(e.Price, e.Volume / FrontRatio);
					}
					else if(e.Price <= e.Bid)
					{
						myProfile.AddBidVolume(e.Price, e.Volume / FrontRatio);
					}
				}
			}
			else if(BarsInProgress == 2)
			{
				if (e.MarketDataType == MarketDataType.Last)
				{
					if (e.Price >= e.Ask)
					{		
						myProfile.AddBidVolume(e.Price, e.Volume / BackRatio);
					}
					else if(e.Price <= e.Bid)
					{
						myProfile.AddAskVolume(e.Price, e.Volume / BackRatio);
					}
				}
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="FrontLeg", Description="Front leg of the contract EG ZN 06-17", Order=1, GroupName="Parameters")]
		public string FrontLeg
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="FrontRatio", Description="Ratio for front leg.  See http://www.cmegroup.com/trading/interest-rates/intercommodity-spread.html", Order=1, GroupName="Parameters")]
		public int FrontRatio
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="BackLeg", Description="Back leg of the contract eg ZB 06-17", Order=2, GroupName="Parameters")]
		public string BackLeg
		{ get; set; }
				
		[NinjaScriptProperty]
		[Display(Name="BackRatio", Description="Ratio for back leg.  See http://www.cmegroup.com/trading/interest-rates/intercommodity-spread.html", Order=1, GroupName="Parameters")]
		public int BackRatio
		{ get; set; }

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
		private SpreadCumulativeDelta[] cacheSpreadCumulativeDelta;
		public SpreadCumulativeDelta SpreadCumulativeDelta(string frontLeg, int frontRatio, string backLeg, int backRatio, CumulativeDeltaTimeframe resetDeltaOn)
		{
			return SpreadCumulativeDelta(Input, frontLeg, frontRatio, backLeg, backRatio, resetDeltaOn);
		}

		public SpreadCumulativeDelta SpreadCumulativeDelta(ISeries<double> input, string frontLeg, int frontRatio, string backLeg, int backRatio, CumulativeDeltaTimeframe resetDeltaOn)
		{
			if (cacheSpreadCumulativeDelta != null)
				for (int idx = 0; idx < cacheSpreadCumulativeDelta.Length; idx++)
					if (cacheSpreadCumulativeDelta[idx] != null && cacheSpreadCumulativeDelta[idx].FrontLeg == frontLeg && cacheSpreadCumulativeDelta[idx].FrontRatio == frontRatio && cacheSpreadCumulativeDelta[idx].BackLeg == backLeg && cacheSpreadCumulativeDelta[idx].BackRatio == backRatio && cacheSpreadCumulativeDelta[idx].ResetDeltaOn == resetDeltaOn && cacheSpreadCumulativeDelta[idx].EqualsInput(input))
						return cacheSpreadCumulativeDelta[idx];
			return CacheIndicator<SpreadCumulativeDelta>(new SpreadCumulativeDelta(){ FrontLeg = frontLeg, FrontRatio = frontRatio, BackLeg = backLeg, BackRatio = backRatio, ResetDeltaOn = resetDeltaOn }, input, ref cacheSpreadCumulativeDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SpreadCumulativeDelta SpreadCumulativeDelta(string frontLeg, int frontRatio, string backLeg, int backRatio, CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.SpreadCumulativeDelta(Input, frontLeg, frontRatio, backLeg, backRatio, resetDeltaOn);
		}

		public Indicators.SpreadCumulativeDelta SpreadCumulativeDelta(ISeries<double> input , string frontLeg, int frontRatio, string backLeg, int backRatio, CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.SpreadCumulativeDelta(input, frontLeg, frontRatio, backLeg, backRatio, resetDeltaOn);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SpreadCumulativeDelta SpreadCumulativeDelta(string frontLeg, int frontRatio, string backLeg, int backRatio, CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.SpreadCumulativeDelta(Input, frontLeg, frontRatio, backLeg, backRatio, resetDeltaOn);
		}

		public Indicators.SpreadCumulativeDelta SpreadCumulativeDelta(ISeries<double> input , string frontLeg, int frontRatio, string backLeg, int backRatio, CumulativeDeltaTimeframe resetDeltaOn)
		{
			return indicator.SpreadCumulativeDelta(input, frontLeg, frontRatio, backLeg, backRatio, resetDeltaOn);
		}
	}
}

#endregion
