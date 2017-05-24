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
	public class Spread : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Shows the difference between two instruments.";
				Name										= "Spread";
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
				IsSuspendedWhileInactive					= true;
				FrontLeg					= string.Empty;
				BackLeg					= string.Empty;
				FrontRatio 				= 3;
				BackRatio				= 1;
				AddPlot(Brushes.Orange, "MySpread");
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
			
			Value[0] = BarsArray[1][0] * FrontRatio - BarsArray[2][0] * BackRatio;
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

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MySpread
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
		private Spread[] cacheSpread;
		public Spread Spread(string frontLeg, int frontRatio, string backLeg, int backRatio)
		{
			return Spread(Input, frontLeg, frontRatio, backLeg, backRatio);
		}

		public Spread Spread(ISeries<double> input, string frontLeg, int frontRatio, string backLeg, int backRatio)
		{
			if (cacheSpread != null)
				for (int idx = 0; idx < cacheSpread.Length; idx++)
					if (cacheSpread[idx] != null && cacheSpread[idx].FrontLeg == frontLeg && cacheSpread[idx].FrontRatio == frontRatio && cacheSpread[idx].BackLeg == backLeg && cacheSpread[idx].BackRatio == backRatio && cacheSpread[idx].EqualsInput(input))
						return cacheSpread[idx];
			return CacheIndicator<Spread>(new Spread(){ FrontLeg = frontLeg, FrontRatio = frontRatio, BackLeg = backLeg, BackRatio = backRatio }, input, ref cacheSpread);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Spread Spread(string frontLeg, int frontRatio, string backLeg, int backRatio)
		{
			return indicator.Spread(Input, frontLeg, frontRatio, backLeg, backRatio);
		}

		public Indicators.Spread Spread(ISeries<double> input , string frontLeg, int frontRatio, string backLeg, int backRatio)
		{
			return indicator.Spread(input, frontLeg, frontRatio, backLeg, backRatio);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Spread Spread(string frontLeg, int frontRatio, string backLeg, int backRatio)
		{
			return indicator.Spread(Input, frontLeg, frontRatio, backLeg, backRatio);
		}

		public Indicators.Spread Spread(ISeries<double> input , string frontLeg, int frontRatio, string backLeg, int backRatio)
		{
			return indicator.Spread(input, frontLeg, frontRatio, backLeg, backRatio);
		}
	}
}

#endregion
