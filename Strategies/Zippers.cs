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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class Zippers : Strategy
	{
		MAX MaxOfPeriod;
		MIN MinOfPeriod;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Zippers";
				Calculate									= Calculate.OnBarClose;
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
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				ZipperLength								= 4;
				ZipperWidth									= 2;
				FadeZippers									= true;
				BarsRequiredToPlot 							= ZipperLength + 1;
			}
			else if (State == State.Configure)
			{
				
				SetProfitTarget(CalculationMode.Ticks, 2);
				SetStopLoss(CalculationMode.Ticks, 4);
				
				MaxOfPeriod = MAX(High, ZipperLength);
				MinOfPeriod = MIN(Low, ZipperLength);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] <= BarsRequiredToPlot)
			{
        		return;
			}
			
			
			double topOfZipper = MaxOfPeriod[0];
			double bottomOfZipper = MinOfPeriod[0];
			
			double topOfLeadin = MaxOfPeriod[1];
			double bottomOfLeadin = MinOfPeriod[1];
			
			double range = topOfZipper - bottomOfZipper;
			double leadin = topOfLeadin - bottomOfLeadin;
			
			
			if(range / TickSize == ZipperWidth && leadin / TickSize == ZipperWidth + 1)
			{				
				//lead in was higher so trend is down.
				if(topOfLeadin > topOfZipper)
				{
					//Enter short limit orders.
					if(FadeZippers)
					{
						EnterLongLimit(bottomOfZipper);
					}
					else
						EnterShortLimit(topOfZipper);
				}
				//lead in was lower so trend is up.
				else if(bottomOfLeadin < bottomOfZipper)
				{
					if(FadeZippers == true)
					{
						EnterShortLimit(topOfZipper);
					}
					else
						EnterLongLimit(bottomOfZipper);
				}
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="ZipperLength", Description="How many bars are neccessary to be considered a zipper.", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public int ZipperLength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="FadeZippers?", Description="Fades zippers based on leadin direction.", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public bool FadeZippers
		{get;set;}

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="ZipperWidth", Description="The range of the zipper.", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public int ZipperWidth
		{ get; set; }
		#endregion

	}
}
