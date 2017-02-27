#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
			
	public class Level2Column : Indicator
	{	
		public class RowData
		{
			public long Liquidity;
			public double Price;
			public MarketDataType DataType;
			public bool Active;
			public RowData(double price, long liquidity, MarketDataType dataType, bool active)
			{
				Liquidity = liquidity;
				Price = price;
				DataType = dataType;
				Active = active;
			}
		}		
		
		public class Profile
		{
			public Dictionary<double, RowData> Data;
			public Double HighestLiquidityPrice;
			public long HighestLiquidity;

			public Profile()
			{
				Data = new Dictionary<double, RowData>();				
			}
			
			public void Update(MarketDepthEventArgs args)
			{
				RowData val;
				
				if(args.Operation == Operation.Update || args.Operation == Operation.Add)
				{
					if (Data.TryGetValue(args.Price, out val))
				    {
			        	val.Price = args.Price;
						val.Liquidity = args.Volume;
						val.DataType = args.MarketDataType;
						val.Active = true;
					}
					else
					{
						Data.Add(args.Price, new RowData(args.Price, args.Volume, args.MarketDataType, true));
					}
				}
					
				else if(args.Operation == Operation.Remove)
				{
					if (Data.TryGetValue(args.Price, out val))
				    {
						val.Active = false;
					}
				}	
				
				FindHighestLiqudity();				
			}
			
			public void FindHighestLiqudity()
			{
				long highestLiquidity = 0;
				Double highestLiquidityPrice = 0;
				
				foreach(KeyValuePair<double, RowData> pair in Data)
				{
					if(pair.Value.Liquidity > highestLiquidity)
					{
						highestLiquidity = pair.Value.Liquidity;
						highestLiquidityPrice = pair.Value.Price;
					}
				}
				
				HighestLiquidity = highestLiquidity;
				HighestLiquidityPrice = HighestLiquidityPrice;
			}
			
		}		
		

		
		#region Variables
		
		
		// ---
		private DateTime lastRender;
		
		// ---
		
		private SimpleFont sf;
		
				
		public Profile myProfile = new Profile();
		
		public int ColumnIndex = 0;
		
		public int MinimumWidth = 0;
		public int ProfileWidth = 0;
		public int MyWidth = 0;
		
		#endregion
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{				
				Description					= "Draws level2 info as a colum next to your chart.";
				Name						= "Level2 Column";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= true;
				IsAutoScale 				= false;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= false;
				BarsRequiredToPlot			= 2;
				ScaleJustification			= ScaleJustification.Right;
				
				textSize		 			= 11;
			}
			else if (State == State.Configure)
			{
				sf = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", textSize);
				
				ZOrder = ChartBars.ZOrder + 1;	
			}
			else if (State == State.Historical)
			{
				if(ChartControl != null)
				{		
					MinimumWidth = getTextWidth("99000") + 6;
					ProfileWidth = MinimumWidth;
					MyWidth = MinimumWidth + ProfileWidth;
										
					foreach (NinjaTrader.Gui.NinjaScript.IndicatorRenderBase indicator in ChartControl.Indicators)
	  				{
						if(indicator is ProfileColumns && indicator != this && indicator.State != State.SetDefaults)
						{
							ColumnIndex++;
						}
					}
					
					ChartControl.Properties.BarMarginRight += MyWidth;
					//ChartControl.Properties.BarMarginRight = 280 * (ColumnIndex+1);
				}
			}
			else if (State == State.Terminated)
            {
                if (ChartControl == null) return;
                ChartControl.Properties.BarMarginRight -= MyWidth;
            }
		}
		
		protected override void OnMarketDepth(MarketDepthEventArgs e)
        {
			myProfile.Update(e);	
			ForceRefresh();
        }
	
		// OnRender
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{			
			if(Bars == null || Bars.Instrument == null || IsInHitTest || CurrentBar < 1) { return; }
			
			lastRender = DateTime.Now;
			
			base.OnRender(chartControl, chartScale);			
			
			try
        	{
				
				foreach(KeyValuePair<double, RowData> row in myProfile.Data)
				{
					drawRow(chartControl, chartScale, row.Value);
				}
				
			}
			catch(Exception e)
	        {

	            Print("FootPrintV2: " + e.Message);
	        }
		}
		
		#region drawProfile
		private void drawRow(ChartControl chartControl, ChartScale chartScale, RowData row)
		{
			// This will be used to control the Font, Style and Size of the font which is used on ALL text used in this sample. 
            TextFormat textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 9f)
            {
                TextAlignment = TextAlignment.Trailing,   //TextAlignment.Leading,
                WordWrapping = WordWrapping.NoWrap
            };
			
			Brush bidColor	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0)); bidColor.Freeze();
			Brush inactiveBidColor	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 0, 0)); bidColor.Freeze();
			Brush askColor	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 255)); askColor.Freeze();
			Brush inactiveAskColor	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 128)); askColor.Freeze();
			SharpDX.Direct2D1.Brush rowBrush = bidColor.ToDxBrush(RenderTarget);;
			
			if(row.DataType == MarketDataType.Ask && row.Active == true)
				rowBrush = askColor.ToDxBrush(RenderTarget);
			if(row.DataType == MarketDataType.Ask && row.Active == false)
				rowBrush = inactiveAskColor.ToDxBrush(RenderTarget);
			if(row.DataType == MarketDataType.Bid && row.Active == true)
				rowBrush = bidColor.ToDxBrush(RenderTarget);
			if(row.DataType == MarketDataType.Bid && row.Active == false)
				rowBrush = inactiveBidColor.ToDxBrush(RenderTarget);
					
					
			double percentage = ((double)row.Liquidity / myProfile.HighestLiquidity);		
			double histogram =  ProfileWidth * percentage;
			//double y1 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price + TickSize)) / 2) + 1;
			//double y2 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price - TickSize)) / 2) - 1;
			double y1 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price + TickSize)) / 2) + 1;
			double y2 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price - TickSize)) / 2) - 1;
			
			SharpDX.RectangleF rect = new SharpDX.RectangleF();
			rect.X      = (float)chartControl.CanvasRight - 200;
			rect.Y      = (float)y1;
			rect.Width  = (float)-(MinimumWidth + histogram);
			rect.Height = (float)Math.Abs(y1 - y2);			
			
			RenderTarget.FillRectangle(rect, rowBrush);
			RenderTarget.DrawText(string.Format("{0}", row.Liquidity), textFormat, rect, Brushes.White.ToDxBrush(RenderTarget));
		}
		#endregion
	
		
		#region Utilities
		
		private DateTime getStartDate(int workDays)
	    {
			int 			dir = workDays < 0 ? -1 : 1;
			DateTime        now = DateTime.UtcNow;
			SessionIterator sit = new SessionIterator(Bars); sit.GetNextSession(now, true);
			DateTime 		act = sit.ActualSessionBegin;
			
		    while(workDays != 0)
		    {
		     	act = act.AddDays(dir);
				
		      	if(act.DayOfWeek != DayOfWeek.Saturday && act.DayOfWeek != DayOfWeek.Sunday)
		      	{
		        	workDays -= dir;
		      	}
		    }
			
		    return act;
	    }
		
		private int getTextWidth(string text)
		{
			float textWidth = 0f;
			
			if(text.Length > 0)
			{
				TextFormat tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
				TextLayout tl = new TextLayout(Core.Globals.DirectWriteFactory, text, tf, ChartPanel.W, ChartPanel.H);
				
				textWidth = tl.Metrics.Width;
				
				tf.Dispose();
				tl.Dispose();
			}
			
			return (int)textWidth;
		}
		
		#endregion

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Text Size", GroupName = "Parameters", Order = 7)]
		public int textSize
		{ get; set; }
		
		#endregion;
		
	}
}

public enum Timeframe { Session, Week, Month, Never };

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Level2Column[] cacheLevel2Column;
		public Level2Column Level2Column(int textSize)
		{
			return Level2Column(Input, textSize);
		}

		public Level2Column Level2Column(ISeries<double> input, int textSize)
		{
			if (cacheLevel2Column != null)
				for (int idx = 0; idx < cacheLevel2Column.Length; idx++)
					if (cacheLevel2Column[idx] != null && cacheLevel2Column[idx].textSize == textSize && cacheLevel2Column[idx].EqualsInput(input))
						return cacheLevel2Column[idx];
			return CacheIndicator<Level2Column>(new Level2Column(){ textSize = textSize }, input, ref cacheLevel2Column);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Level2Column Level2Column(int textSize)
		{
			return indicator.Level2Column(Input, textSize);
		}

		public Indicators.Level2Column Level2Column(ISeries<double> input , int textSize)
		{
			return indicator.Level2Column(input, textSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Level2Column Level2Column(int textSize)
		{
			return indicator.Level2Column(Input, textSize);
		}

		public Indicators.Level2Column Level2Column(ISeries<double> input , int textSize)
		{
			return indicator.Level2Column(input, textSize);
		}
	}
}

#endregion
