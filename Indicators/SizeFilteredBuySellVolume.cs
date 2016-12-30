// 
// Copyright (C) 2016, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class SizeFilteredBuySellVolume : Indicator
	{
		private double	buys;
		private double	sells;
		private int activeBar = 0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description				= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionBuySellVolume;
				Name					= "Size Filtered " + NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameBuySellVolume;
				BarsRequiredToPlot		= 1;
				Calculate				= Calculate.OnEachTick;
				DrawOnPricePanel		= false;
				IsOverlay				= false;
				DisplayInDataBox		= true;

				// Plots will overlap each other no matter which one of these comes first
				// in NT8, we would add the Sells first in code and then Buys, and the "Sells" was always in front of the buys.
				AddPlot(new Stroke(Brushes.DarkCyan,	2), PlotStyle.Bar, NinjaTrader.Custom.Resource.BuySellVolumeBuys);
				AddPlot(new Stroke(Brushes.Crimson,		2), PlotStyle.Bar, NinjaTrader.Custom.Resource.BuySellVolumeSells);
			}
			else if (State == State.Historical)
			{
				if (Calculate != Calculate.OnEachTick)
					Draw.TextFixed(this, "NinjaScriptInfo", string.Format(NinjaTrader.Custom.Resource.NinjaScriptOnBarCloseError, Name), TextPosition.BottomRight);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{			
			if(e.MarketDataType == MarketDataType.Last && e.Volume >= Size)
			{				
				if(e.Price >= e.Ask)
				{
					Buys[0] += e.Volume;
				}
				else if (e.Price <= e.Bid)
				{
					Sells[0] += e.Volume;
				}
			}	
		}
		
//		protected override void OnBarUpdate()
//		{
//			if (CurrentBar < activeBar || CurrentBar <= BarsRequiredToPlot)
//				return;

//			// New Bar has been formed
//			// - Assign last volume counted to the prior bar
//			// - Reset volume count for new bar
//			if (CurrentBar != activeBar)
//			{
//				Sells[1] = sells;
//				Buys[1] = buys + sells;
//				buys = 0;
//				sells = 0;
//				activeBar = CurrentBar;
//			}

//			Sells[0] = sells;
//			Buys[0] = buys + sells;
//		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Sells
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Buys
		{
			get { return Values[0]; }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name = "Volume Size", GroupName = "Parameters", Order = 1)]
		public int Size
		{ get; set; }
		#endregion
		
		

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SizeFilteredBuySellVolume[] cacheSizeFilteredBuySellVolume;
		public SizeFilteredBuySellVolume SizeFilteredBuySellVolume(int size)
		{
			return SizeFilteredBuySellVolume(Input, size);
		}

		public SizeFilteredBuySellVolume SizeFilteredBuySellVolume(ISeries<double> input, int size)
		{
			if (cacheSizeFilteredBuySellVolume != null)
				for (int idx = 0; idx < cacheSizeFilteredBuySellVolume.Length; idx++)
					if (cacheSizeFilteredBuySellVolume[idx] != null && cacheSizeFilteredBuySellVolume[idx].Size == size && cacheSizeFilteredBuySellVolume[idx].EqualsInput(input))
						return cacheSizeFilteredBuySellVolume[idx];
			return CacheIndicator<SizeFilteredBuySellVolume>(new SizeFilteredBuySellVolume(){ Size = size }, input, ref cacheSizeFilteredBuySellVolume);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SizeFilteredBuySellVolume SizeFilteredBuySellVolume(int size)
		{
			return indicator.SizeFilteredBuySellVolume(Input, size);
		}

		public Indicators.SizeFilteredBuySellVolume SizeFilteredBuySellVolume(ISeries<double> input , int size)
		{
			return indicator.SizeFilteredBuySellVolume(input, size);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SizeFilteredBuySellVolume SizeFilteredBuySellVolume(int size)
		{
			return indicator.SizeFilteredBuySellVolume(Input, size);
		}

		public Indicators.SizeFilteredBuySellVolume SizeFilteredBuySellVolume(ISeries<double> input , int size)
		{
			return indicator.SizeFilteredBuySellVolume(input, size);
		}
	}
}

#endregion
