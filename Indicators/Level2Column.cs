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
			
	public class Level2Column : ColumnPanel
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
		
		public class OrderBook
		{
			public Dictionary<double, RowData> Data;
			public Double HighestLiquidityPrice;
			public long HighestLiquidity;

			public OrderBook()
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
				
				
		private OrderBook MyOrderBook = new OrderBook();
		
		public int ColumnIndex = 0;
		
		#endregion
		
		protected override void OnStateChange()
		{
			base.OnStateChange();
			
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
				
				AskColor    	 	= Brushes.CornflowerBlue;
				BidColor    	 	= Brushes.Orange;
				InactiveAskColor    	 	= Brushes.Blue;
				InactiveBidColor    	 	= Brushes.DarkOrange;
				textSize			= 11;
				ResizableWidth = 30;
			}
			else if (State == State.Configure)
			{				
				ZOrder = ChartBars.ZOrder + 1;	
			}
		}
		
		protected override void OnMarketDepth(MarketDepthEventArgs e)
        {
			MyOrderBook.Update(e);	
			ForceRefresh();
        }
	
		// OnRender
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{			
			base.OnRender(chartControl, chartScale);	
			if(Bars == null || Bars.Instrument == null || CurrentBar < 1) { return; }
			
			LastRender = DateTime.Now;			
			
			try
        	{
				
				foreach(KeyValuePair<double, RowData> row in MyOrderBook.Data)
				{
					drawRow(chartControl, chartScale, row.Value);
				}
				
			}
			catch(Exception e)
	        {

	            Print("Level2Column: " + e.Message);
	        }
		}
		
		private void drawRow(ChartControl chartControl, ChartScale chartScale, RowData row)
		{
			Brush brushColor = AskColor;
			
			//Determine color of this row.
			if(row.DataType == MarketDataType.Ask && row.Active == true)
				brushColor = AskColor;
			if(row.DataType == MarketDataType.Ask && row.Active == false)
				brushColor = InactiveAskColor;
			if(row.DataType == MarketDataType.Bid && row.Active == true)
				brushColor = BidColor;
			if(row.DataType == MarketDataType.Bid && row.Active == false)
				brushColor = InactiveBidColor;
					
			//Calculate Histogram width.
			double percentage = ((double)row.Liquidity / MyOrderBook.HighestLiquidity);		
			double histogram =  ResizableWidth * percentage;
			
			//Calculate Cell Properties
			double y1 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price + TickSize)) / 2) + 1;
			double y2 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price - TickSize)) / 2) - 1;
			
			SharpDX.RectangleF rect = new SharpDX.RectangleF();
			rect.X      = (float)chartControl.CanvasRight - Position;
			rect.Y      = (float)y1;
			rect.Width  = (float)-(MinimumWidth + histogram);
			rect.Height = (float)Math.Abs(y1 - y2);			
			
			//Draw the row.
			using(SharpDX.Direct2D1.Brush rowBrush =  brushColor.ToDxBrush(RenderTarget))
			{
				RenderTarget.FillRectangle(rect, rowBrush);
			}

			if(rect.Height > this.MinimumTextHeight)
			{
				RenderTarget.DrawText(string.Format("{0}", row.Liquidity), textFormat, rect, TextColor.ToDxBrush(RenderTarget));
			}
		}
		
		#region Properties

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Bid Color", GroupName = "Parameters", Order = 9)]
		public Brush BidColor
		{ get; set; }
		
		[Browsable(false)]
		public string BidColorSerializable
		{
			get { return Serialize.BrushToString(BidColor); }
			set { BidColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Ask Color", GroupName = "Parameters", Order = 9)]
		public Brush AskColor
		{ get; set; }
		
		[Browsable(false)]
		public string AskColorSerializable
		{
			get { return Serialize.BrushToString(AskColor); }
			set { AskColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Inactive Bid Color", GroupName = "Parameters", Order = 9)]
		public Brush InactiveBidColor
		{ get; set; }
		
		[Browsable(false)]
		public string InactiveBidColorSerializable
		{
			get { return Serialize.BrushToString(InactiveBidColor); }
			set { InactiveBidColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Inactive Ask Color", GroupName = "Parameters", Order = 9)]
		public Brush InactiveAskColor
		{ get; set; }
		
		[Browsable(false)]
		public string InactiveAskColorSerializable
		{
			get { return Serialize.BrushToString(InactiveAskColor); }
			set { InactiveAskColor = Serialize.StringToBrush(value); }
		}
		
		#endregion;
		
	}
}

public enum Timeframe { Session, Week, Month, Never };









