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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators.Poncho;
using NinjaTrader.NinjaScript.DrawingTools;
using PriceActionSwing.Base;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class BigFish : Strategy
	{
		SizeFilteredBuySellVolume Whales;
		SizeFilteredBuySellVolume AllFish;
		SMA Trend;
		
		protected override void OnStateChange()
		{
			Print(DateTime.Now + ": Current State is State."+State);
			
			if (State == State.SetDefaults)
			{
				Description									= @"Do what big fish do.";
				Name										= "BigFish";
				Calculate									= Calculate.OnEachTick;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 2;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				NumberOfFish								= 5000;
				BigFishSize									= 100;
				StopLoss 									= 5;
				ProfitTarget								= 15;
				MovingAveragePeriod							= 50;
			}
			else if (State == State.Configure)
			{

				
				//SetTrailStop(CalculationMode.Ticks, 15);
				SetStopLoss(CalculationMode.Ticks, StopLoss);
				SetProfitTarget(CalculationMode.Ticks, ProfitTarget);
				Whales = SizeFilteredBuySellVolume(BigFishSize);
				AllFish = SizeFilteredBuySellVolume(1);		
				
				AddDataSeries(BarsPeriodType.Day, 1);	
				Trend = SMA(BarsArray[1], MovingAveragePeriod);
			}
			else if(State == State.DataLoaded)
			{
				//Trend = PriceActionSwingOscillator(BarsArray[1], SwingStyle.Standard, 7, 20, false, true, true);
			}
		}

		protected override void OnBarUpdate()
		{			
			if (CurrentBars[0] <= BarsRequiredToPlot || CurrentBars[1] <= BarsRequiredToPlot)
			{
        		return;
			}
			
			double BigBuys = Whales.Buys[0];
			double BigSells = Whales.Sells[0];
			double AllBuys = AllFish.Buys[0];
			double AllSells = AllFish.Sells[0];
		    double NormalBuys = AllBuys - BigBuys;
			double NormalSells = AllSells - BigSells;
			
			if(BarsInProgress == 1)
			{
				Print("BarsInProgress is 1");
				
				//Trend = PriceActionSwingOscillator(BarsArray[1], SwingStyle.Standard, 7, 20, false, true, true);
				//double buyStrength = Trend.SwingLow[0];
				//double sellStrength = Trend.RTDownTrend[0];
			
				//Print(buyStrength.ToString());
			}

			
			if(Trend[0] < Close[0])
			{					
				if(AllBuys > NumberOfFish && AllBuys > AllSells * 2 && BigBuys != 0 && BigSells == 0)
				{
					Print("Uptrend");
					
					EnterLong(BigBuys + "/" + BigSells + "!" + AllBuys + "/" + AllSells);
				}
				
				if(AllSells > NumberOfFish && AllSells < AllBuys * 1.25)
				{
					ExitLong(AllSells.ToString());
				}
				
				if(AllSells > NumberOfFish && AllSells > AllBuys * 2 && BigSells != 0 && BigBuys == 0)
				{				
					ExitLong(AllSells.ToString());
				}
				
				if(AllBuys > NumberOfFish && AllBuys < AllSells * 1.25)
				{
					ExitLong(AllSells.ToString());
				}				
			}
			else
			{
				
				if(AllBuys > NumberOfFish && AllBuys > AllSells * 2 && BigBuys != 0 && BigSells == 0)
				{
					ExitShort(BigBuys + "/" + BigSells + "!" + AllBuys + "/" + AllSells);
				}
				
				if(AllSells > NumberOfFish && AllSells < AllBuys * 1.25)
				{
					ExitShort(AllSells.ToString());
				}
				
				if(AllSells > NumberOfFish && AllSells > AllBuys * 2 && BigSells != 0 && BigBuys == 0)
				{
					Print("Downtrend");				
				
					EnterShort(AllSells.ToString());
				}
				
				if(AllBuys > NumberOfFish && AllBuys < AllSells * 1.25)
				{
					ExitShort(AllSells.ToString());
				}
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Number Of Fish", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public int NumberOfFish
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Big Fish Size", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public int BigFishSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Moving Average Period", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public int MovingAveragePeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Stop Loss", Order=3, GroupName="NinjaScriptStrategyParameters")]
		public int StopLoss
		{ get; set; }

		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Profit Target", Order=3, GroupName="NinjaScriptStrategyParameters")]
		public int ProfitTarget
		{ get; set; }
		

		#endregion

	}
}
