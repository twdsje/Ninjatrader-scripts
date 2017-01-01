#region Using declarations
using System;
using System.Drawing;
using System.Collections;
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
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX.Direct2D1;
using SharpDX;
using SharpDX.DirectWrite;
//using System.Windows.Forms;

using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
#endregion

#region Enums
public enum FootPrintBarEnum
{
	BidAsk,
	VolumeDelta
}
public enum FootPrintBarColorEnum
{
	Saturation,
	VolumeBar,
	Solid,
	None
}
public enum ClosePriceEnum
{
	TextColor,
	Rectangle,
	None
}
public enum HighestVolumeEnum
{
	Rectangle,
	None
}
//public enum ZOrderType
//{
//	Normal,
//	AlwaysDrawnFirst
//}


#endregion
	
//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class FootprintChart : Indicator
	{
		#region Variable and Structure Declarations
 
		private int barSpacing = 90;
		private int barWidth = 43;
		private bool setChartProperties = true;
		private string displayName = null;
		FootPrintBarEnum footPrintBarType = FootPrintBarEnum.BidAsk;
		FootPrintBarColorEnum footPrintBarColor = FootPrintBarColorEnum.Saturation;
		ClosePriceEnum closePriceIndicator = ClosePriceEnum.TextColor;
		HighestVolumeEnum highestVolumeIndicator = HighestVolumeEnum.Rectangle;
		NinjaTrader.Gui.Tools.SimpleFont textFont = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", 12);
		
		public class FootprintBar
		{
			public double Volume {get; set;}
			public double SessionVolume {get; set;}
			public double Delta {get; set;}
			public double SessionDelta {get;set;}
			
			public string BarDelta1;// {get;set;}
			
			public double LastHit {get;set;}
			public Dictionary<double, BidAskVolume> Footprints {get;set;}
			
			public FootprintBar(FootprintBar previous)
			{
				Footprints = new Dictionary<double, BidAskVolume>();
				SessionVolume = previous.SessionVolume;
				SessionDelta = previous.SessionDelta;
				Volume = 0;
				Delta = 0;
				LastHit = 0;
			}
			
			public FootprintBar()
			{
				Footprints = new Dictionary<double, BidAskVolume>();
				SessionVolume = 0;
				Volume = 0;
				Delta = 0;
				BarDelta1 = "Bar Delta";
				LastHit = 0;
			}
			
		
			public void AddAskVolume(double price, double volume)
			{
				BidAskVolume val;
				
				if (Footprints.TryGetValue(price, out val))
			    {
			        double askVolume = val.askVolume + volume;
					double bidVolume = val.bidVolume;
					double currentVolume = val.currentVolume + volume;	
					
					Footprints[price] = new BidAskVolume(currentVolume, askVolume, bidVolume, price);
			    }
			    else
			    {
			        Footprints.Add(price, new BidAskVolume(volume, volume, 0, price));
			    }
	
				Volume = Volume + volume;
				SessionVolume = SessionVolume + volume;
				Delta = Delta + volume;
				SessionDelta = SessionDelta + volume;				
			}
			
			public void AddBidVolume(double price, double volume)
			{
				BidAskVolume val;
				
				if (Footprints.TryGetValue(price, out val))
			    {
					double bidVolume = val.bidVolume + volume;
					double askVolume = val.askVolume;
					double currentVolume = val.currentVolume + volume;
					Footprints[price] = new BidAskVolume(currentVolume, askVolume, bidVolume, price);
				}
				else
				{
					Footprints.Add(price, new BidAskVolume(volume, 0, volume, price));
			    }
				
				Volume = Volume + volume;
				SessionVolume = SessionVolume + volume;
				Delta = Delta - volume;
				SessionDelta = SessionDelta - volume;	
			}
		}
				
		public struct BidAskVolume
		{
			public double currentVolume;
			public double askVolume;
			public double bidVolume;
			public double Price;
			
			public BidAskVolume(double cv, double av, double bv, double price)
			{
				currentVolume = cv;
				askVolume = av;
				bidVolume = bv;
				Price = price;
				
			}
		}		
		
		                                                       
		private FootprintBar CurrentFootprint = new FootprintBar();
		private List<FootprintBar> FootprintBars = new List<FootprintBar>();
		
		private Dictionary<double, BidAskVolume> bidAskVolume = new Dictionary<double, BidAskVolume>();
		
		double tmpAskVolume;
		double tmpBidVolume;
		double tmpCurrentVolume;
 		int	   chartBarIndex;
		
		double barVolume;			// bar volume
		double barDelta;			// bar delta
		double lastHit = 0;			// meant to show(text colour) if it was a buy or sell on the close of each bar   ... current bar[0] ok but not previous closes .... under construction
		
		#endregion
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				
			if (State == State.Historical)
			  {
			  }				
				
				
				Description							= @"Displays candlestick bars with coloured histogram of buy & sell volumes and more...";
				Name								= "FootprintChart";		// by Seth 20.12.2016
				Calculate							= Calculate.OnEachTick;
				IsOverlay							= true;
				DisplayInDataBox					= true;
				DrawOnPricePanel					= true;
				DrawHorizontalGridLines				= true;
				DrawVerticalGridLines				= true;
				PaintPriceMarkers					= true;
				ScaleJustification					= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsAutoScale 						= true;
				IsSuspendedWhileInactive			= true;
				
				// default color values
				BarDeltaDownColor 					= Brushes.Pink;
				BarDeltaUpColor 					= Brushes.LightGreen;
				SessionDeltaDownColor 				= Brushes.Tomato;
				SessionDeltaUpColor 				= Brushes.LimeGreen;
				FooterFontColor 					= Brushes.Black;
				BarVolumeBackgroundColor			= Brushes.LightBlue;
				SessionVolumeBackgroundColor		= Brushes.DodgerBlue;
				FootBrintParClosePriceColor		 	= Brushes.Gold;
				FootBrintParHighestVolumeColor 		= Brushes.LightBlue;
				FootBrintParTextColor 				= Brushes.Black;
				FootPrintBarUpColor 				= Brushes.LimeGreen;
				FootPrintBarDownColor 				= Brushes.Red;
				
				// current price line
				LineSpreadColor						= Brushes.Black;
				LineShortColor						= Brushes.Red;
				LineLongColor						= Brushes.Green;
				LineStyle							= DashStyleHelper.Dot;
				LineWidth							= 2;
				
				// default boolean values
				ShowCurrentPrice			= false;
				ShowFooter 					= true;							
				ShowCandleStickBars 		= true;
				ShowBodyBar					= false;
				ShowWicks 					= true;
			}
			else if (State == State.Configure)
			{		
				setChartProperties = true;
							
				// create a better display name
				displayName = Name +  " (" + Instrument.FullName + ", " + BarsPeriod.Value + " " + BarsPeriod.BarsPeriodType.ToString() + ", Bar Type: " + footPrintBarType.ToString() + ", Color Type: " + footPrintBarColor.ToString() + ")";
			}
		}

		public override string DisplayName
        {
            get { return (displayName != null ? displayName : Name); }
        }

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			
			
//				if (IsFirstTickOfBar)
			
//				if (Bars.IsFirstBarOfSession)
//				{
//////					CurrentFootprint = new FootprintBar();
//////					FootprintBars.Add(CurrentFootprint);
//				SessionDelta = 0;
//				}
		
			
			
			if (e.MarketDataType == MarketDataType.Last){
							
				lastHit=0;

				if (e.Price >= e.Ask)
				{					
					CurrentFootprint.AddAskVolume(e.Price, e.Volume);
					
					if(ShowCurrentPrice)
					Draw.HorizontalLine(this, "CurrPrice", false, e.Price, LineShortColor, LineStyle, LineWidth);
				}
				else if(e.Price <= e.Bid)
				{
					CurrentFootprint.AddBidVolume(e.Price, e.Volume);
					
					if(ShowCurrentPrice)
					Draw.HorizontalLine(this, "CurrPrice", false, e.Price, LineLongColor, LineStyle, LineWidth);
				}
			}
		}
		
		protected override void OnBarUpdate()
		{
			if (!Bars.IsTickReplay)
				Draw.TextFixed(this, "warning msg", "WARNING: Tick Replay must be enabled for FootPrintChart to display historical values.", TextPosition.TopRight);

			BarBrushes[0] = Brushes.Transparent;
			CandleOutlineBrushes[0] = Brushes.Transparent;
			
			if (IsFirstTickOfBar)
			{
				if (Bars.IsFirstBarOfSession)
				{
					CurrentFootprint = new FootprintBar();
					FootprintBars.Add(CurrentFootprint);
				}
				else
				{
					CurrentFootprint = new FootprintBar(CurrentFootprint);
					FootprintBars.Add(CurrentFootprint);
				}				
			}
		}		

		public override void OnCalculateMinMax()
		{			 
			// exit if ChartBars has not yet been initialized
			if (ChartBars == null)
				return;
			
            // int barLowPrice and barHighPrice to min and max double vals
            double barLowPrice = double.MaxValue;
            double barHighPrice = double.MinValue;

            // loop through the bars visible on the chart
            for (int index = ChartBars.FromIndex; index <= ChartBars.ToIndex; index++)
            {
                // get min/max of bar high/low values
                barLowPrice = Math.Min(barLowPrice, Low.GetValueAt(index));
                barHighPrice = Math.Max(barHighPrice, High.GetValueAt(index));
            }
			
			// number of ticks between high/low price
			double priceTicks = (barHighPrice - barLowPrice) * (1 / TickSize);
			
			// number of ticks on the chart panel based on the chart panel height and text font size
			double panelTicks = ChartPanel.H / (textFont.Size + 4);
			
			// number of ticks we'll need to add to high and low price to auto adjust the chart
			double ticksNeeded = (panelTicks - priceTicks) / 2;

			// calc min and max chart prices
			MinValue = barLowPrice - (ticksNeeded * TickSize);
            MaxValue = barHighPrice + (ticksNeeded * TickSize);
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			// set the bar spacing and width of the chart, we only want to do this once and not on every render
			if (setChartProperties) {
				chartControl.Properties.BarDistance = barSpacing;
				chartControl.BarWidth = barWidth;
				setChartProperties = false;
			}
						
			// create the TextFormat structure for the footprint bars
			TextFormat footPrintBarFont = new TextFormat(new SharpDX.DirectWrite.Factory(),
														 textFont.Family.ToString(),
														 textFont.Bold ? SharpDX.DirectWrite.FontWeight.Bold : SharpDX.DirectWrite.FontWeight.Normal,
														 textFont.Italic ? SharpDX.DirectWrite.FontStyle.Italic : SharpDX.DirectWrite.FontStyle.Normal,
														 (float)textFont.Size); 
			
			for (chartBarIndex = ChartBars.FromIndex; chartBarIndex <= ChartBars.ToIndex; chartBarIndex++)
		    {	
				Print("Drawing bar: " + chartBarIndex);
				
				// current bar prices
				double barClosePrice = ChartBars.Bars.GetClose(chartBarIndex);
		        double barOpenPrice	 = ChartBars.Bars.GetOpen(chartBarIndex);
				double barHighPrice	 = ChartBars.Bars.GetHigh(chartBarIndex);
		        double barLowPrice	 = ChartBars.Bars.GetLow(chartBarIndex);
							
				barVolume = ChartBars.Bars.GetVolume(chartBarIndex);
							
				
				// current bar X and Y points
				int	x 			= chartControl.GetXByBarIndex(ChartBars, chartBarIndex) - (int)chartControl.BarWidth;
				float barX		= chartControl.GetXByBarIndex(ChartBars, chartBarIndex);
				float barOpenY	= chartScale.GetYByValue(barOpenPrice);
				float barCloseY	= chartScale.GetYByValue(barClosePrice);
				float barHighY	= chartScale.GetYByValue(barHighPrice);
				float barLowY	= chartScale.GetYByValue(barLowPrice);
				
				// vectors and rectangle for the candlestick bars
				Vector2	point0	= new Vector2();
				Vector2	point1	= new Vector2();
				RectangleF rect	= new RectangleF();
									
				// calculate the physical font size for the selected font
				GlyphTypeface gtf = new GlyphTypeface();
				System.Windows.Media.Typeface t_face = new System.Windows.Media.Typeface(new System.Windows.Media.FontFamily(textFont.Family.ToString()), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);				
				t_face.TryGetGlyphTypeface(out gtf);
				
				// chart drawing starts from the current price line down, so for our footprint rectangles we need to offset our footprint bar drawings up a bit so they align with the price marker
				double fontOffset = (gtf.CapsHeight * textFont.Size) * 0.6 * 1.8;
				double rectangleOffset = (gtf.CapsHeight * textFont.Size) * 1.8;
				
				// init totalDelta and maxVolumePrice
				double totalDelta = 0;
				double maxVolumePrice = 0;	
				
				double sessionDelta = 0;
				double sessionVolume = 0;
				
// BARS
				#region FootPrint Bars
				if (chartBarIndex == ChartBars.Count)
				{					
					double maxVolume = double.MinValue;;
					double maxAskVolume = double.MinValue;
					double maxBidVolume = double.MinValue;
					double footPrintBarTopVolume = double.MinValue;
					
					foreach (KeyValuePair<double, BidAskVolume> kvp in CurrentFootprint.Footprints)
	            	{
						if ((kvp.Value.askVolume + kvp.Value.bidVolume) > maxVolume) {
							maxVolume = kvp.Value.askVolume + kvp.Value.bidVolume;
							maxVolumePrice = kvp.Key;
						}
						
						if (footPrintBarType == FootPrintBarEnum.BidAsk) {
							maxBidVolume = Math.Max(maxBidVolume, kvp.Value.bidVolume);
							maxAskVolume = Math.Max(maxAskVolume, kvp.Value.askVolume);
						} else {
							maxBidVolume = Math.Max(maxBidVolume, (kvp.Value.askVolume + kvp.Value.bidVolume));
							maxAskVolume = Math.Max(maxAskVolume, (kvp.Value.askVolume - kvp.Value.bidVolume));
						}
					
						int y = chartScale.GetYByValue(kvp.Key) - (int)(fontOffset);							
						
						// determine the bar opacity
						double curr_percent = 100 * (barVolume / maxVolume);
						double curr_opacity = Math.Round((curr_percent / 100) * 0.8, 1);
						curr_opacity = curr_opacity == 0 ? 0.1 : curr_opacity;

						// set the color based on the volume direction
						SharpDX.Direct2D1.Brush footPrintBarColor = FootPrintBarUpColor.ToDxBrush(RenderTarget);
						if (kvp.Value.askVolume < kvp.Value.bidVolume)
							footPrintBarColor = FootPrintBarDownColor.ToDxBrush(RenderTarget);
											
						// draw the background color
						if (FootPrintBarColor == FootPrintBarColorEnum.VolumeBar) {
														
							double ratioAsk = 0;
							double ratioBid = 0;
							
							if (maxAskVolume != 0)
								ratioAsk = 1f - (kvp.Value.askVolume / maxAskVolume);
							
							if (maxBidVolume != 0)
								ratioBid = 1f - (kvp.Value.bidVolume / maxBidVolume);
								
							// determine the width of the rectangle based on the bid/ask volume
							double width = (chartControl.BarWidth - (chartControl.BarWidth * ratioBid)) + (chartControl.BarWidth - (chartControl.BarWidth * ratioAsk));
								
							RenderTarget.FillRectangle(new RectangleF(x + (float)(chartControl.BarWidth * ratioBid), y, (float)(width), (float)(rectangleOffset)), footPrintBarColor);
						} else if (FootPrintBarColor == FootPrintBarColorEnum.Saturation) {
							footPrintBarColor.Opacity = (float)curr_opacity;
							RenderTarget.FillRectangle(new RectangleF(x, y, (float)(chartControl.BarWidth * 2), (float)(rectangleOffset)), footPrintBarColor);
						} else if (FootPrintBarColor == FootPrintBarColorEnum.Solid) {
							RenderTarget.FillRectangle(new RectangleF(x, y, (float)(chartControl.BarWidth * 2), (float)(rectangleOffset)), footPrintBarColor);
						}
							
						// create the bid/ask or volume/delta strings to show on the chart
						string bidStr = null;
						string askStr = null;
						if (footPrintBarType == FootPrintBarEnum.BidAsk) {
							bidStr = kvp.Value.bidVolume.ToString();
							askStr = kvp.Value.askVolume.ToString();
						} else {
							bidStr = barVolume.ToString();
							askStr = barDelta.ToString();
						}
						

						// draw the bid footprint bar string
						footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
						if (kvp.Key == barClosePrice && closePriceIndicator == ClosePriceEnum.TextColor && lastHit==1)
							RenderTarget.DrawText(bidStr, footPrintBarFont, new RectangleF(barX - 5 - (float)chartControl.BarWidth, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParClosePriceColor.ToDxBrush(RenderTarget));
						else
							RenderTarget.DrawText(bidStr, footPrintBarFont, new RectangleF(barX - 5 - (float)chartControl.BarWidth, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParTextColor.ToDxBrush(RenderTarget));
													
						// draw the ask footprint bar string
						footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
						if (kvp.Key == barClosePrice && closePriceIndicator == ClosePriceEnum.TextColor && lastHit==2)
							RenderTarget.DrawText(askStr, footPrintBarFont, new RectangleF(barX + 5, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParClosePriceColor.ToDxBrush(RenderTarget));
						else
							RenderTarget.DrawText(askStr, footPrintBarFont, new RectangleF(barX + 5, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParTextColor.ToDxBrush(RenderTarget));
					}	
				}
				else
				{
					double maxVolume = double.MinValue;			
					double maxAskVolume = double.MinValue;
					double maxBidVolume = double.MinValue;
							
					Print("break1");
					Print(FootprintBars.Count);
					try
					{
					Print(FootprintBars[chartBarIndex-1].Footprints.Count);
					}
					catch(Exception e)
					{
						Print(e.Message);
					}
					
					foreach (KeyValuePair<double, BidAskVolume> kvp in FootprintBars[chartBarIndex].Footprints)
	            	{
						Print("break2");
						BidAskVolume t = kvp.Value;
						
						if ((t.askVolume + t.bidVolume) > maxVolume) {
							maxVolume = t.askVolume + t.bidVolume;
							maxVolumePrice = t.Price;
						}
						
						if (footPrintBarType == FootPrintBarEnum.BidAsk) {
							maxBidVolume = Math.Max(maxBidVolume, t.bidVolume);
							maxAskVolume = Math.Max(maxAskVolume, t.askVolume);
						} else {
							maxBidVolume = Math.Max(maxBidVolume, (t.bidVolume + t.askVolume));
							maxAskVolume = Math.Max(maxAskVolume, (t.askVolume - t.askVolume));
						}
					
						// y value of where our drawing will start minus our fontOffset
						int y = chartScale.GetYByValue(t.Price) - (int)(fontOffset);
												
						// sum up the totalDelta, currentVolume, and delta values
						totalDelta += t.askVolume;
						totalDelta -= t.bidVolume;
						barVolume = t.askVolume + t.bidVolume;
																
						double curr_percent = 100 * (barVolume / maxVolume);
						double curr_opacity = Math.Round((curr_percent / 100) * 0.8, 1);
						curr_opacity = curr_opacity == 0 ? 0.1 : curr_opacity;
						
						// set the color based on the volume direction
						SharpDX.Direct2D1.Brush footPrintBarColor = FootPrintBarUpColor.ToDxBrush(RenderTarget);
						if (t.askVolume < t.bidVolume)
							footPrintBarColor = FootPrintBarDownColor.ToDxBrush(RenderTarget);
						
						// draw the background color
						if (FootPrintBarColor == FootPrintBarColorEnum.VolumeBar) {
														
							double ratioAsk = 0;
							double ratioBid = 0;
							
							if (maxAskVolume != 0)
								ratioAsk = 1f - (t.askVolume / maxAskVolume);
							
							if (maxBidVolume != 0)
								ratioBid = 1f - (t.bidVolume / maxBidVolume);
														
							// determine the width of the rectangle based on the bid/ask volume
							double width = (chartControl.BarWidth - (chartControl.BarWidth * ratioBid)) + (chartControl.BarWidth - (chartControl.BarWidth * ratioAsk));
								
							RenderTarget.FillRectangle(new RectangleF(x + (float)(chartControl.BarWidth * ratioBid), y, (float)(width), (float)(rectangleOffset)), footPrintBarColor);
						} else if (FootPrintBarColor == FootPrintBarColorEnum.Saturation) {
							footPrintBarColor.Opacity = (float)curr_opacity;
							RenderTarget.FillRectangle(new RectangleF(x, y, (float)(chartControl.BarWidth * 2), (float)(rectangleOffset)), footPrintBarColor);
						} else if (FootPrintBarColor == FootPrintBarColorEnum.Solid) {
							RenderTarget.FillRectangle(new RectangleF(x, y, (float)(chartControl.BarWidth * 2), (float)(rectangleOffset)), footPrintBarColor);
						}
							
						// create the bid/ask or volume/delta strings to show on the chart
						string bidStr = null;
						string askStr = null;
						if (footPrintBarType == FootPrintBarEnum.BidAsk) {
							bidStr = t.bidVolume.ToString();
							askStr = t.askVolume.ToString();
						} else {
							bidStr = barVolume.ToString();
							askStr = barDelta.ToString();
						}
						
						// draw the bid footprint bar string
						footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
						if (t.Price == barClosePrice && closePriceIndicator == ClosePriceEnum.TextColor)
							RenderTarget.DrawText(bidStr, footPrintBarFont, new RectangleF(barX - 5 - (float)chartControl.BarWidth, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParClosePriceColor.ToDxBrush(RenderTarget));
						else
							RenderTarget.DrawText(bidStr, footPrintBarFont, new RectangleF(barX - 5 - (float)chartControl.BarWidth, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParTextColor.ToDxBrush(RenderTarget));
													
						// draw the ask footprint bar string
						footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
						if (t.Price == barClosePrice && closePriceIndicator == ClosePriceEnum.TextColor)
							RenderTarget.DrawText(askStr, footPrintBarFont, new RectangleF(barX + 5, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParClosePriceColor.ToDxBrush(RenderTarget));
						else
							RenderTarget.DrawText(askStr, footPrintBarFont, new RectangleF(barX + 5, y, (float)chartControl.BarWidth, (float)(rectangleOffset)),
								FootBrintParTextColor.ToDxBrush(RenderTarget));	
					}
				}
				Print("break3");
				#endregion

				#region CandleStick Bars
				if (ShowCandleStickBars || ShowBodyBar)
				{
					if (Math.Abs(barOpenY - barCloseY) < 0.0000001) {
						// draw doji bar if no movement between open and close
						if (ShowCandleStickBars) {
							point0.X = x ;
							point0.Y = barCloseY;
							point1.X = x - 1;
							point1.Y = barCloseY;
							
							RenderTarget.DrawLine(point0, point1, ChartBars.Properties.ChartStyle.Stroke.BrushDX, 1);
						}
						
						if (ShowBodyBar) {
							point0.X = barX - 3;
							point0.Y = barCloseY;
							point1.X = barX + 3;
							point1.Y = barCloseY;
							
							RenderTarget.DrawLine(point0, point1, ChartBars.Properties.ChartStyle.Stroke.BrushDX, 1);
						}
					} else {
						if (ShowCandleStickBars) {
							rect.X = barX - 2;//(int)chartControl.BarWidth-7;	
							rect.Y = Math.Min(barCloseY, barOpenY);		
							rect.Width	= 4;
							rect.Height	= Math.Max(barOpenY, barCloseY) - Math.Min(barCloseY, barOpenY);
							
							// set the candlestick color based on open and close price
							System.Windows.Media.Brush candleStickColor = barClosePrice >= barOpenPrice ? ChartBars.Properties.ChartStyle.UpBrush : ChartBars.Properties.ChartStyle.DownBrush;

							// draw the candlestick
							RenderTarget.FillRectangle(rect, candleStickColor.ToDxBrush(RenderTarget));
							
							// draw the candlestick outline color, the color and width come from the main chart properties
							RenderTarget.DrawRectangle(rect, ChartBars.Properties.ChartStyle.Stroke.BrushDX, ChartBars.Properties.ChartStyle.Stroke.Width);
						}
						
						if (ShowBodyBar) {
							rect.X		= barX - (int)chartControl.BarWidth;
							rect.Y		= Math.Min(barCloseY, barOpenY) ;
							rect.Width	= barWidth * 2;
							rect.Height	= (Math.Max(barOpenY, barCloseY) - Math.Min(barCloseY, barOpenY)) + (float)rectangleOffset;
											
							// set the candlestick color based on open and close price
							System.Windows.Media.Brush candleStickColor = barClosePrice >= barOpenPrice ? ChartBars.Properties.ChartStyle.UpBrush : ChartBars.Properties.ChartStyle.DownBrush;

							// draw the candlestick outline color, the color and width come from the main chart properties
							RenderTarget.DrawRectangle(rect, candleStickColor.ToDxBrush(RenderTarget), 2);
						}
					}
					
					if (ShowWicks) {						
						// high wick
						if (barHighY < Math.Min(barOpenY, barCloseY)) {
							if (ShowCandleStickBars) {
								point0.X = barX ;
								point0.Y = barHighY;
								point1.X = barX ;
								point1.Y = Math.Min(barOpenY, barCloseY);
							
								// draw the high wick, the color and width come from the main chart properties
								RenderTarget.DrawLine(point0, point1, ChartBars.Properties.ChartStyle.Stroke2.BrushDX, ChartBars.Properties.ChartStyle.Stroke2.Width);
							}

							if (ShowBodyBar) {
								point0.X = barX;
								point0.Y = barHighY;
								point1.X = barX;
								
								if (Math.Abs(barOpenY - barCloseY) < 0.0000001)
									point1.Y = Math.Max(barOpenY, barCloseY);
								else
									point1.Y = Math.Max(barOpenY, barCloseY) + ((float)rectangleOffset / 2);
							}	
						}

						// low wick
						if (barLowY > Math.Max(barOpenY, barCloseY)) {
							if (ShowCandleStickBars) {
								point0.X = barX ;
								point0.Y = barLowY;
								point1.X = barX ;
								point1.Y = Math.Max(barOpenY, barCloseY);

								// draw the low wick, the color and width come from the main chart properties
								RenderTarget.DrawLine(point0, point1, ChartBars.Properties.ChartStyle.Stroke2.BrushDX, ChartBars.Properties.ChartStyle.Stroke2.Width);
							}							
							
							if (ShowBodyBar) {
								point0.X = barX;
								point0.Y = barLowY;
								point1.X = barX;
								
								if (Math.Abs(barOpenY - barCloseY) < 0.0000001)
									point1.Y = Math.Max(barOpenY, barCloseY);
								else
									point1.Y = Math.Max(barOpenY, barCloseY) + ((float)rectangleOffset / 2);

							}							
						}
					}
				}
				#endregion
				
				
// FOOTER				
				#region Title Box Footer
				if (ShowFooter) {
					SharpDX.Direct2D1.Brush volumeColor;

					// set the volume background color
					volumeColor = BarVolumeBackgroundColor.ToDxBrush(RenderTarget);
					
					
						
				float xBar = chartControl.GetXByBarIndex(ChartBars, chartBarIndex) + (float)(chartControl.BarWidth);
	//				
//					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 57), (float)(chartControl.BarWidth * 2), (float)rectangleOffset), volumeColor);
//					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 42), (float)(chartControl.BarWidth * 4), (float)rectangleOffset), volumeColor);
//					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 27), (float)(chartControl.BarWidth * 4), (float)rectangleOffset), volumeColor);
//					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 12), (float)(chartControl.BarWidth * 4), (float)rectangleOffset), volumeColor);
									
					// draw the Bar Delta text string
//					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
//					RenderTarget.DrawText(FootprintBars[chartBarIndex].ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 57), (float)(chartControl.BarWidth), (float)rectangleOffset),
//							FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
					
//					// draw the Session Delta text string
//		//				if(xBar != chartBarIndex)
					
					
	// testing ideas
	//				RenderTarget.FillRectangle(new RectangleF(FootprintBars[chartBarIndex].BarDelta1, (float)(ChartPanel.H - 57), (float)(chartControl.BarWidth * 2), (float)rectangleOffset), volumeColor);

					
					
//					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
//					RenderTarget.DrawText("Session Delta".ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 42), (float)(chartControl.BarWidth * 4), (float)rectangleOffset),
//							FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
					
//					// draw the Bar Volume text string
//					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
//					RenderTarget.DrawText("Bar Volume".ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 27), (float)(chartControl.BarWidth * 4), (float)rectangleOffset),
//							FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
					
//					// draw the Session Volume text string
//					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
//					RenderTarget.DrawText("Session Volume".ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 12), (float)(chartControl.BarWidth * 4), (float)rectangleOffset),
//							FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
				}
				#endregion
				
				#region Bar Delta Footer
				if (ShowFooter) {
					SharpDX.Direct2D1.Brush deltaColor;
					
					// set the background color based on the delta number
					if (totalDelta > 0)
						deltaColor = BarDeltaUpColor.ToDxBrush(RenderTarget);
					else
						deltaColor = BarDeltaDownColor.ToDxBrush(RenderTarget);
						
					float xBar = chartControl.GetXByBarIndex(ChartBars, chartBarIndex - 1) + (float)(chartControl.BarWidth+4 );
					
					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 57), (float)(chartControl.BarWidth * 2), (float)rectangleOffset), deltaColor);
									
					// draw the bar delta string
					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
					RenderTarget.DrawText(FootprintBars[chartBarIndex].Delta.ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 57), (float)(chartControl.BarWidth * 2), (float)rectangleOffset),
							FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
				}
				#endregion

				#region Session delta Footer
				if (ShowFooter) {
					SharpDX.Direct2D1.Brush deltaColor;

					// set the session delta background color
					if (sessionDelta > 0)
						deltaColor = SessionDeltaUpColor.ToDxBrush(RenderTarget);
					else
						deltaColor = SessionDeltaDownColor.ToDxBrush(RenderTarget);
						
					float xBar = chartControl.GetXByBarIndex(ChartBars, chartBarIndex-1) + (float)(chartControl.BarWidth+4);
					
					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 42), (float)(chartControl.BarWidth * 2), (float)rectangleOffset), deltaColor);
					
					// draw the session delta string
					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;

					RenderTarget.DrawText(FootprintBars[chartBarIndex].SessionDelta.ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 42), (float)(chartControl.BarWidth * 2), (float)rectangleOffset),
						FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
				}
				#endregion
				
				#region Bar Volume Footer
				if (ShowFooter) {
					SharpDX.Direct2D1.Brush volumeColor;

					// set the volume background color
					volumeColor = BarVolumeBackgroundColor.ToDxBrush(RenderTarget);
						
					float xBar = chartControl.GetXByBarIndex(ChartBars, chartBarIndex - 1) + (float)(chartControl.BarWidth+4);
					
					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 27), (float)(chartControl.BarWidth * 2), (float)rectangleOffset), volumeColor);
									
					// draw the bar volume string
					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
					RenderTarget.DrawText(FootprintBars[chartBarIndex].Volume.ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 27), (float)(chartControl.BarWidth * 2), (float)rectangleOffset),
							FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
				}
				#endregion
				
				#region Session Volume Footer
				if (ShowFooter) {
					SharpDX.Direct2D1.Brush volumeColor;

					// set the volume background color
					volumeColor = SessionVolumeBackgroundColor.ToDxBrush(RenderTarget);
						
					float xBar = chartControl.GetXByBarIndex(ChartBars, chartBarIndex - 1) + (float)(chartControl.BarWidth+4);
					
					RenderTarget.FillRectangle(new RectangleF(xBar, (float)(ChartPanel.H - 12), (float)(chartControl.BarWidth * 2), (float)rectangleOffset), volumeColor);
									
					// draw the session volume string
					footPrintBarFont.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
					RenderTarget.DrawText(FootprintBars[chartBarIndex].SessionVolume.ToString(), footPrintBarFont, new RectangleF(xBar, (float)(ChartPanel.H - 12), (float)(chartControl.BarWidth * 2), (float)rectangleOffset),
							FooterFontColor.ToDxBrush(RenderTarget), DrawTextOptions.None, MeasuringMode.GdiClassic);
				}
				
				Print("drew bar: " + chartBarIndex);
				#endregion
			}
		}
		
		#region Properties
		
		#region Chart Properties
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Bar Spacing", Description="Sets the space between the footprint bars.", Order=1, GroupName="1. Chart Properties")]
		public int BarSpacing
		{
			get { return barSpacing; }
			set {barSpacing = Math.Max(1, value);}
		}
	
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Bar Width", Description="Sets the width of the footprint bars.", Order=2, GroupName="1. Chart Properties")]
		public int BarWidth
		{
			get { return barWidth; }
			set {barWidth = Math.Max(1, value);}
		}
				
	    [NinjaScriptProperty]
	    [Display(Name = "Bar Font", Description = "Text font for the chart bars.", Order = 4, GroupName = "1. Chart Properties")]
	    public NinjaTrader.Gui.Tools.SimpleFont TextFont
	    {
	        get { return textFont; }
	        set { textFont = value; }
	    }
		#endregion
		
		#region CandleStick Properties
		[NinjaScriptProperty]
		[Display(Name="Show CandleStick", Order=1, Description = "Show the CandleStick.", GroupName="2. CandleStick Bar Properties")]
		public bool ShowCandleStickBars
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Outline in FootPrint Bar", Order=2, Description = "Shows a candlestick outline in the FootPrint bar body.", GroupName="2. CandleStick Bar Properties")]
		public bool ShowBodyBar
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Bar Wicks", Order=3, Description = "Show High/Low Wicks", GroupName="2. CandleStick Bar Properties")]
		public bool ShowWicks
		{ get; set; }
		
		#endregion
		
		#region FootPrint Bar Properties
		[NinjaScriptProperty]
		[Display(Name="Bar Type", Description="Shows either Bid/Ask volume or Volume/Delta", Order=1, GroupName = "3. Candle Properties")]
		public FootPrintBarEnum FootPrintBarType
		{
			get { return footPrintBarType; }
			set { footPrintBarType = value; }
		}

		[NinjaScriptProperty]
		[Display(Name="Bar Color", Description="Shows either a volume bar or a saturation color backgroud.", Order=2, GroupName = "3. Candle Properties")]
		public FootPrintBarColorEnum FootPrintBarColor
		{
			get { return footPrintBarColor; }
			set { footPrintBarColor = value; }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Text Color", Description = "Color for the text string.", Order = 3, GroupName = "3. Candle Properties")]
		public System.Windows.Media.Brush FootBrintParTextColor		
		{ get; set; }
		
		[Browsable(false)]
		public string FootBrintParTextColorSerialize
		{
			get { return Serialize.BrushToString(FootBrintParTextColor); }
   			set { FootBrintParTextColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Volume Up Bar Color", Description = "Color for the up volume bars.", Order = 4, GroupName = "3. Candle Properties")]
		public System.Windows.Media.Brush FootPrintBarUpColor		
		{ get; set; }
		
		[Browsable(false)]
		public string FootPrintBarUpColorSerialize
		{
			get { return Serialize.BrushToString(FootPrintBarUpColor); }
   			set { FootPrintBarUpColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Volume Down Bar Color", Description = "Color for the down volume bars.", Order = 5, GroupName = "3. Candle Properties")]
		public System.Windows.Media.Brush FootPrintBarDownColor		
		{ get; set; }
		
		[Browsable(false)]
		public string FootPrintBarDownColorSerialize
		{
			get { return Serialize.BrushToString(FootPrintBarDownColor); }
   			set { FootPrintBarDownColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Close Indicator", Description="Indicates the close price in text or rectangle.", Order=6, GroupName = "3. Candle Properties")]
		public ClosePriceEnum ClosePriceIndicator
		{
			get { return closePriceIndicator; }
			set { closePriceIndicator = value; }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Bar Close Color", Description = "Color for the close price.", Order = 7, GroupName = "3. Candle Properties")]
		public System.Windows.Media.Brush FootBrintParClosePriceColor		
		{ get; set; }
		
		[Browsable(false)]
		public string FootBrintParClosePriceColorSerialize
		{
			get { return Serialize.BrushToString(FootBrintParClosePriceColor); }
   			set { FootBrintParClosePriceColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name="High Volume Indicator", Description="Indicates the high volume in text or rectangle.", Order=8, GroupName = "3. Candle Properties")]
		public HighestVolumeEnum HighVolumeIndicator
		{
			get { return highestVolumeIndicator; }
			set { highestVolumeIndicator = value; }
		}
		
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Highest Volume Indicator Color", Description = "Color for the high volume rectangle.", Order = 9, GroupName = "3. Candle Properties")]
		public System.Windows.Media.Brush FootBrintParHighestVolumeColor		
		{ get; set; }
		
		[Browsable(false)]
		public string FootBrintParHighestVolumeBarColorSerialize
		{
			get { return Serialize.BrushToString(FootBrintParHighestVolumeColor); }
   			set { FootBrintParHighestVolumeColor = Serialize.StringToBrush(value); }
		}				
		#endregion
				
		#region Footer Properties
		[NinjaScriptProperty]
		[Display(Name="Show Footer Delta", Order=1, GroupName="4. Footer Properties")]
		public bool ShowFooter
		{ get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Text Color", Order = 2, GroupName = "4. Footer Properties")]
		public System.Windows.Media.Brush FooterFontColor		
		{ get; set; }
		
		[Browsable(false)]
		public string FooterFontColorSerialize
		{
			get { return Serialize.BrushToString(FooterFontColor); }
   			set { FooterFontColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Bar Delta Up Background Color", Order = 3, GroupName = "4. Footer Properties")]
		public System.Windows.Media.Brush BarDeltaUpColor		
		{ get; set; }
		
		[Browsable(false)]
		public string BarDeltaUpColorSerialize
		{
			get { return Serialize.BrushToString(BarDeltaUpColor); }
   			set { BarDeltaUpColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Bar Delta Down Background Color", Order = 4, GroupName = "4. Footer Properties")]
		public System.Windows.Media.Brush BarDeltaDownColor		
		{ get; set; }
		
		[Browsable(false)]
		public string BarDeltaDownColorSerialize
		{
			get { return Serialize.BrushToString(BarDeltaDownColor); }
   			set { BarDeltaDownColor = Serialize.StringToBrush(value); }
		}
		
		
		
		
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Session Delta Up Background Color", Order = 5, GroupName = "4. Footer Properties")]
		public System.Windows.Media.Brush SessionDeltaUpColor		
		{ get; set; }
		
		[Browsable(false)]
		public string SessionDeltaUpColorSerialize
		{
			get { return Serialize.BrushToString(SessionDeltaUpColor); }
   			set { SessionDeltaUpColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Session Delta Down Background Color", Order = 6, GroupName = "4. Footer Properties")]
		public System.Windows.Media.Brush SessionDeltaDownColor		
		{ get; set; }
		
		[Browsable(false)]
		public string SessionDeltaDownColorSerialize
		{
			get { return Serialize.BrushToString(SessionDeltaDownColor); }
   			set { SessionDeltaDownColor = Serialize.StringToBrush(value); }
		}
		#endregion
				
				[XmlIgnore]
				[Display(ResourceType = typeof(Custom.Resource), Name = "Bar Volume Background Color", Order = 7, GroupName = "4. Footer Properties")]
				public System.Windows.Media.Brush BarVolumeBackgroundColor		
				{ get; set; }
				
				[Browsable(false)]
				public string BarVolumeBackgroundColorSerialize
				{
					get { return Serialize.BrushToString(BarVolumeBackgroundColor); }
		   			set { BarVolumeBackgroundColor = Serialize.StringToBrush(value); }
				}
				[XmlIgnore]
				[Display(ResourceType = typeof(Custom.Resource), Name = "Session Volume Background Color", Order = 8, GroupName = "4. Footer Properties")]
				public System.Windows.Media.Brush SessionVolumeBackgroundColor		
				{ get; set; }
				
				[Browsable(false)]
				public string SessionVolumeBackgroundColorSerialize
				{
					get { return Serialize.BrushToString(SessionVolumeBackgroundColor); }
		   			set { SessionVolumeBackgroundColor = Serialize.StringToBrush(value); }
				}
				#endregion
				
				#region PriceLine
				[NinjaScriptProperty]
				[Display(Name="Show current price", Order=1, GroupName="7. CurrentPrice")]
				public bool ShowCurrentPrice
				{ get; set; }
		
				[XmlIgnore]
				[Display(Name="Spread Color", Description="Price SPREAD color", Order=2, GroupName="7. CurrentPrice")]
				public System.Windows.Media.Brush LineSpreadColor		
				{ get; set; }

				[Browsable(false)]
				public string LineSpreadColorSerializable
				{
					get { return Serialize.BrushToString(LineSpreadColor); }
					set { LineSpreadColor = Serialize.StringToBrush(value); }
				}			
				[XmlIgnore]
				[Display(Name="Short Color", Description="Price SHORT color", Order=3, GroupName="7. CurrentPrice")]
				public System.Windows.Media.Brush LineShortColor		
				{ get; set; }

				[Browsable(false)]
				public string LineShortColorSerializable
				{
					get { return Serialize.BrushToString(LineShortColor); }
					set { LineShortColor = Serialize.StringToBrush(value); }
				}			

				[XmlIgnore]
				[Display(Name="Long Color", Description="Price LONG color", Order=4, GroupName="7. CurrentPrice")]
				public System.Windows.Media.Brush LineLongColor		
				{ get; set; }

				[Browsable(false)]
				public string LineLongColorSerializable
				{
					get { return Serialize.BrushToString(LineLongColor); }
					set { LineLongColor = Serialize.StringToBrush(value); }
				}			


				[NinjaScriptProperty]
				[Display(Name="LineStyle", Description="Price Line Style", Order=5, GroupName="7. CurrentPrice")]
				public DashStyleHelper LineStyle
				{ get; set; }

				[Range(1, int.MaxValue)]
				[NinjaScriptProperty]
				[Display(Name="LineWidth", Description="Price Line Width", Order=6, GroupName="7. CurrentPrice")]
				public int LineWidth
				{ get; set; }

				#endregion							
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FootprintChart[] cacheFootprintChart;
		public FootprintChart FootprintChart(int barSpacing, int barWidth, NinjaTrader.Gui.Tools.SimpleFont textFont, bool showCandleStickBars, bool showBodyBar, bool showWicks, FootPrintBarEnum footPrintBarType, FootPrintBarColorEnum footPrintBarColor, ClosePriceEnum closePriceIndicator, HighestVolumeEnum highVolumeIndicator, bool showFooter, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth)
		{
			return FootprintChart(Input, barSpacing, barWidth, textFont, showCandleStickBars, showBodyBar, showWicks, footPrintBarType, footPrintBarColor, closePriceIndicator, highVolumeIndicator, showFooter, showCurrentPrice, lineStyle, lineWidth);
		}

		public FootprintChart FootprintChart(ISeries<double> input, int barSpacing, int barWidth, NinjaTrader.Gui.Tools.SimpleFont textFont, bool showCandleStickBars, bool showBodyBar, bool showWicks, FootPrintBarEnum footPrintBarType, FootPrintBarColorEnum footPrintBarColor, ClosePriceEnum closePriceIndicator, HighestVolumeEnum highVolumeIndicator, bool showFooter, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth)
		{
			if (cacheFootprintChart != null)
				for (int idx = 0; idx < cacheFootprintChart.Length; idx++)
					if (cacheFootprintChart[idx] != null && cacheFootprintChart[idx].BarSpacing == barSpacing && cacheFootprintChart[idx].BarWidth == barWidth && cacheFootprintChart[idx].TextFont == textFont && cacheFootprintChart[idx].ShowCandleStickBars == showCandleStickBars && cacheFootprintChart[idx].ShowBodyBar == showBodyBar && cacheFootprintChart[idx].ShowWicks == showWicks && cacheFootprintChart[idx].FootPrintBarType == footPrintBarType && cacheFootprintChart[idx].FootPrintBarColor == footPrintBarColor && cacheFootprintChart[idx].ClosePriceIndicator == closePriceIndicator && cacheFootprintChart[idx].HighVolumeIndicator == highVolumeIndicator && cacheFootprintChart[idx].ShowFooter == showFooter && cacheFootprintChart[idx].ShowCurrentPrice == showCurrentPrice && cacheFootprintChart[idx].LineStyle == lineStyle && cacheFootprintChart[idx].LineWidth == lineWidth && cacheFootprintChart[idx].EqualsInput(input))
						return cacheFootprintChart[idx];
			return CacheIndicator<FootprintChart>(new FootprintChart(){ BarSpacing = barSpacing, BarWidth = barWidth, TextFont = textFont, ShowCandleStickBars = showCandleStickBars, ShowBodyBar = showBodyBar, ShowWicks = showWicks, FootPrintBarType = footPrintBarType, FootPrintBarColor = footPrintBarColor, ClosePriceIndicator = closePriceIndicator, HighVolumeIndicator = highVolumeIndicator, ShowFooter = showFooter, ShowCurrentPrice = showCurrentPrice, LineStyle = lineStyle, LineWidth = lineWidth }, input, ref cacheFootprintChart);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FootprintChart FootprintChart(int barSpacing, int barWidth, NinjaTrader.Gui.Tools.SimpleFont textFont, bool showCandleStickBars, bool showBodyBar, bool showWicks, FootPrintBarEnum footPrintBarType, FootPrintBarColorEnum footPrintBarColor, ClosePriceEnum closePriceIndicator, HighestVolumeEnum highVolumeIndicator, bool showFooter, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth)
		{
			return indicator.FootprintChart(Input, barSpacing, barWidth, textFont, showCandleStickBars, showBodyBar, showWicks, footPrintBarType, footPrintBarColor, closePriceIndicator, highVolumeIndicator, showFooter, showCurrentPrice, lineStyle, lineWidth);
		}

		public Indicators.FootprintChart FootprintChart(ISeries<double> input , int barSpacing, int barWidth, NinjaTrader.Gui.Tools.SimpleFont textFont, bool showCandleStickBars, bool showBodyBar, bool showWicks, FootPrintBarEnum footPrintBarType, FootPrintBarColorEnum footPrintBarColor, ClosePriceEnum closePriceIndicator, HighestVolumeEnum highVolumeIndicator, bool showFooter, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth)
		{
			return indicator.FootprintChart(input, barSpacing, barWidth, textFont, showCandleStickBars, showBodyBar, showWicks, footPrintBarType, footPrintBarColor, closePriceIndicator, highVolumeIndicator, showFooter, showCurrentPrice, lineStyle, lineWidth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FootprintChart FootprintChart(int barSpacing, int barWidth, NinjaTrader.Gui.Tools.SimpleFont textFont, bool showCandleStickBars, bool showBodyBar, bool showWicks, FootPrintBarEnum footPrintBarType, FootPrintBarColorEnum footPrintBarColor, ClosePriceEnum closePriceIndicator, HighestVolumeEnum highVolumeIndicator, bool showFooter, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth)
		{
			return indicator.FootprintChart(Input, barSpacing, barWidth, textFont, showCandleStickBars, showBodyBar, showWicks, footPrintBarType, footPrintBarColor, closePriceIndicator, highVolumeIndicator, showFooter, showCurrentPrice, lineStyle, lineWidth);
		}

		public Indicators.FootprintChart FootprintChart(ISeries<double> input , int barSpacing, int barWidth, NinjaTrader.Gui.Tools.SimpleFont textFont, bool showCandleStickBars, bool showBodyBar, bool showWicks, FootPrintBarEnum footPrintBarType, FootPrintBarColorEnum footPrintBarColor, ClosePriceEnum closePriceIndicator, HighestVolumeEnum highVolumeIndicator, bool showFooter, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth)
		{
			return indicator.FootprintChart(input, barSpacing, barWidth, textFont, showCandleStickBars, showBodyBar, showWicks, footPrintBarType, footPrintBarColor, closePriceIndicator, highVolumeIndicator, showFooter, showCurrentPrice, lineStyle, lineWidth);
		}
	}
}

#endregion
