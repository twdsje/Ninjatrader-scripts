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
using SharpDX;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
			
	public class ProfileColumns : Indicator
	{	
		public struct RowData
		{
			public double TotalVolume;
			public double AskVolume;
			public double BidVolume;
			public double DeltaVolume;
			public double Price;
			
			public RowData(double totalVolume, double askVolume, double bidVolume, double price)
			{
				TotalVolume = totalVolume;
				AskVolume = askVolume;
				BidVolume = bidVolume;
				DeltaVolume = bidVolume - askVolume;
				Price = price;
				
			}
		}		
		
		public class Profile
		{
			public Dictionary<double, RowData> ProfileData;
			public double v = 0.0;

			public Profile()
			{
				ProfileData = new Dictionary<double, RowData>();				
			}
			
			public void AddAskVolume(double price, double volume)
			{
				RowData val;
				
				if (ProfileData.TryGetValue(price, out val))
			    {
			        double askVolume = val.AskVolume + volume;
					double bidVolume = val.BidVolume;
					double totalVolume = val.TotalVolume + volume;	
					
					ProfileData[price] = new RowData(totalVolume, askVolume, bidVolume, price);
			    }
			    else
			    {
			        ProfileData.Add(price, new RowData(volume, volume, 0, price));
			    }			
			}
			
			public void AddBidVolume(double price, double volume)
			{
				RowData val;
				
				if (ProfileData.TryGetValue(price, out val))
			    {
					double bidVolume = val.BidVolume + volume;
					double askVolume = val.AskVolume;
					double totalVolume = val.TotalVolume + volume;
					ProfileData[price] = new RowData(totalVolume, askVolume, bidVolume, price);
				}
				else
				{
					ProfileData.Add(price, new RowData(volume, 0, volume, price));
			    }
			}
		}		
		

		
		#region Variables
		
//		private int rightsidemargin = 600;
		
		
		
		private double ask = 0.0;
		private double bid = 0.0;
//		private double spread = 0.0;
		private double cls = 0.0;
		private double vol = 0.0;
		private double tmp = 0.0;
		
		private double min = 0.0;
		private double max = 0.0;
		private double rng = 0.0;
		private double off = 0.0;
		private double dif = 0.0;
		
		// ---
		private DateTime lastRender;
		
		// ---
		
		private SimpleFont sf;
		
				
		public Profile myProfile = new Profile();
		
		private Series<Profile> Profiles;
		
		private SessionIterator sessionIterator;
		
		public int ColumnIndex = 0;
		
		#endregion
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{				
				Description					= "Draws profile as a colum next to your chart.";
				Name						= "ProfileColumns";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= true;
				IsAutoScale 				= false;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= false;
				BarsRequiredToPlot			= 2;
				ScaleJustification			= ScaleJustification.Right;
				
//me
//				BarDeltaDownColor 					= Brushes.Pink;
//				BarDeltaUpColor 					= Brushes.LightGreen;
				
				cellColor 		 = Brushes.LightGray; cellColor.Freeze();
				
		//		cellColor 			= new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)); cellColor.Freeze();
				highColor 		 	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 72, 72)); highColor.Freeze();
				currColor		 	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(130, 130, 130)); currColor.Freeze();
				markColor			= Brushes.White;	
				markOpacity		 	= 0.05f;
				textColor		 	= Brushes.Black;
				textSize		 	= 11;
				askColor    	 	= Brushes.YellowGreen;
				bidColor    	 	= Brushes.Tomato;
				volumeColor    		= Brushes.Wheat;
				highestvolumeColor  = Brushes.Orange;
//				deltaColor    		= Brushes.Orange;		// profile ladder delta color
				pocColor			= new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 154, 205)); pocColor.Freeze();
				minImbalance	 	= 100.0;
				minRatio     	 	= 0.3;
				autoScroll       	= true;
				setOulineColor   	= true;
				showDelta		 	= true;
				showBidAsk		 	= true;
				showProfile		 	= true;
				showPoc			 	= true;
				showUnfinished	 	= true;
				fadeCells		 	= true;
				fadeText		 	= false;
				indicatorVersion 	= "1.5 | Nov. 2016";
				
				
//me				
	  
				// current price line
				LineSpreadColor						= Brushes.Black;
				LineShortColor						= Brushes.Red;
				LineLongColor						= Brushes.Green;
				LineStyle							= DashStyleHelper.Dot;
				LineWidth							= 2;
			}
			else if (State == State.Configure)
			{
				sf = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", textSize);
				
				Profiles = new Series<Profile>(this, MaximumBarsLookBack.Infinite);
				
				ZOrder = ChartBars.ZOrder - 1;
				
				if(!Bars.IsTickReplay)
				{
					Draw.TextFixed(this, "tickReplay", "Please enable Tick Replay!", TextPosition.TopRight);
				}				
				
				//AddDataSeries(BarsPeriodType.Day, 1);
				sessionIterator = new SessionIterator(Bars);
				
				ChartObjectCollection<NinjaTrader.Gui.NinjaScript.IndicatorRenderBase> indicatorCollection = ChartControl.Indicators;
				
				foreach (NinjaTrader.Gui.NinjaScript.IndicatorRenderBase indicator in indicatorCollection)
  				{
					if(indicator is ProfileColumns && indicator != this && indicator.State != State.SetDefaults)
					{
						ColumnIndex++;
					}
				}
				
				Print("I am: " + ColumnIndex.ToString());
				
				ChartControl.Properties.BarMarginRight = 280 * (ColumnIndex+1);
				
			}
			
						
		}

		protected override void OnBarUpdate()
		{
			
			if(CurrentBars[0] <= BarsRequiredToPlot) return;

			//Reset profile on session week or day.
			if(IsFirstTickOfBar && ResetProfileOn != Timeframe.Never)
			{
				DateTime previous = sessionIterator.GetTradingDay(Time[1]);
				DateTime current = sessionIterator.GetTradingDay(Time[0]);
				
				//Reset profile on daily basis.
				if(ResetProfileOn == Timeframe.Session && !current.DayOfWeek.Equals(previous.DayOfWeek))
				{				
					myProfile = new Profile();
				}
				
				//Reset profile on weekly basis.
				else if(ResetProfileOn == Timeframe.Week && current.DayOfWeek.CompareTo(previous.DayOfWeek) < 0)
				{
					myProfile = new Profile();
				}
				
				//Reset profile on monthly basis.
				else if(ResetProfileOn != Timeframe.Month && !current.Month.Equals(previous.Month))
				{
					myProfile = new Profile();
				}
			}
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
		
		// OnRender
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{			
			if(Bars == null || Bars.Instrument == null || IsInHitTest || CurrentBar < 1) { return; }
			
			lastRender = DateTime.Now;
			
			base.OnRender(chartControl, chartScale);			
			
			try
        	{
				drawProfile(chartControl, chartScale);
			}
			catch(Exception e)
	        {

	            Print("FootPrintV2: " + e.Message);
	        }
		}
		
		#region drawProfile
		private void drawProfile(ChartControl chartControl, ChartScale chartScale)
		{
			Profile currProfile = myProfile;
			
//			if(Profiles.IsValidDataPointAt(ChartBars.ToIndex))
//			{
//				currProfile = Profiles.GetValueAt(ChartBars.ToIndex);
//			}
//			else
//			{
//				return;
//			}
			
//			if(currProfile == null) 	 { return; }
//			if(currProfile.ProfileData.Count == 0) { return; }
			
			SharpDX.Direct2D1.AntialiasMode oldAntialiasMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			
			int imbWidth = getTextWidth("99000") + 6;
			int proWidth = imbWidth * 4;
			
			
			
//me   move Profile to the left
			//int   rx = chartControl.CanvasRight - 280;
			int   rx = chartControl.CanvasRight - (280 * ColumnIndex);
			
			
			
			
			int   x1 = 0;
			int   x2 = 0;
			float y1 = 0;
			float y2 = 0;
			float tx = 0;
			float ty = 0;
			
			double poc = getPoc(currProfile.ProfileData);
			double dta = getDelta(currProfile.ProfileData);
			
			
			double prc = BarsArray[0].GetClose(ChartBars.ToIndex);
			
			double maxPrc = currProfile.ProfileData.Keys.Max();
			double minPrc = currProfile.ProfileData.Keys.Min();
			double curPrc = maxPrc;
			double maxVol = 0.0;
			
			foreach(KeyValuePair<double, RowData> rd in currProfile.ProfileData)
			{
				maxVol = (currProfile.ProfileData[rd.Key].TotalVolume > maxVol) ? currProfile.ProfileData[rd.Key].TotalVolume : maxVol;
			}
			
			if(maxVol == 0.0) { return; }
			
			double currRatio = 0.0;
			double prevRatio = 0.0;
			
			ChartControlProperties chartProps = chartControl.Properties;
			ChartPanel 			   chartPanel = chartControl.ChartPanels[chartScale.PanelIndex];
			
			SharpDX.Direct2D1.Brush backBrush = chartProps.ChartBackground.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush cellBrush = cellColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush highBrush = highColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush currBrush = currColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush textBrush = textColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush markBrush = markColor.ToDxBrush(RenderTarget);
			
			SharpDX.Direct2D1.Brush askBrush = askColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush bidBrush = bidColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush volumeBrush = volumeColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush highestvolumeBrush = highestvolumeColor.ToDxBrush(RenderTarget);
//			SharpDX.Direct2D1.Brush deltaBrush = deltaColor.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush pocBrush = pocColor.ToDxBrush(RenderTarget);
			
			SharpDX.RectangleF rect = new SharpDX.RectangleF();
			SharpDX.Vector2    vec1 = new SharpDX.Vector2();
			SharpDX.Vector2    vec2 = new SharpDX.Vector2();
			
			TextLayout tl;
			TextFormat tf;
			
//me		
//			TextLayout tla;
		
			SharpDX.DirectWrite.FontWeight fw;
			
			GlyphTypeface gtf 					= new GlyphTypeface();
			System.Windows.Media.Typeface tFace = new System.Windows.Media.Typeface(new System.Windows.Media.FontFamily(sf.Family.ToString()), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            tFace.TryGetGlyphTypeface(out gtf);
			
			
			foreach(KeyValuePair<double, RowData> rd in currProfile.ProfileData)
			{
				
				curPrc = Instrument.MasterInstrument.RoundToTickSize(rd.Key);
				
				if(!currProfile.ProfileData.ContainsKey(curPrc))
				{
					curPrc -= TickSize;
					continue;
				}
				
				y1 = ((chartScale.GetYByValue(curPrc) + chartScale.GetYByValue(curPrc + TickSize)) / 2) + 1;
				y2 = ((chartScale.GetYByValue(curPrc) + chartScale.GetYByValue(curPrc - TickSize)) / 2) - 1;
				
	 			
	// ask(BUY) - rect
				
				rect.X      = (float)(rx - imbWidth)+38;
				rect.Y      = (float)y1;
				rect.Width  = (float)imbWidth;
				rect.Height = (float)Math.Abs(y1 - y2);
				
				if(curPrc == poc)
				{
					RenderTarget.DrawRectangle(rect, highBrush);
					RenderTarget.FillRectangle(rect, highBrush);
				}
				else
				{
					RenderTarget.DrawRectangle(rect, cellBrush);
					RenderTarget.FillRectangle(rect, askBrush);
				}
				
				if(curPrc == prc)
				{
					highBrush.Opacity = 0.33f;
					RenderTarget.DrawRectangle(rect, highBrush);
					
					rect.Width  = rect.Width  - 1f;
					rect.Height = rect.Height - 1f;
					
					//rectangle  fill color for current bid(buy) price
					RenderTarget.FillRectangle(rect, highBrush); //askBrush
					highBrush.Opacity = 1.0f;
					
					rect.Width  = rect.Width  + 1f;
					rect.Height = rect.Height + 1f;
				}

////me			//max buy volume	
				if(currProfile.ProfileData[curPrc].AskVolume >= 100)
				{
		//			RenderTarget.DrawRectangle(rect, askBrush);
					RenderTarget.FillRectangle(rect, askBrush);
				}
				
				
	// ask - outline
				
				if(GetCurrentAsk() == curPrc && curPrc == prc)
				{
					vec1.X = rect.X - 1;
					vec1.Y = rect.Y;
					
					vec2.X = rect.X + rect.Width;
					vec2.Y = rect.Y;
					
					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
					
					vec1.X = rect.X - 1;
					vec1.Y = rect.Y + rect.Height;
					
					vec2.X = rect.X + rect.Width;
					vec2.Y = rect.Y + rect.Height;
					
					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
				}
				
				// ask - text
				
				fw = (curPrc == poc) ? SharpDX.DirectWrite.FontWeight.UltraBold : SharpDX.DirectWrite.FontWeight.Normal;
				
				tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), fw, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
				
				tf.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				
				tl = new TextLayout(Core.Globals.DirectWriteFactory, currProfile.ProfileData[curPrc].AskVolume.ToString(), tf, rect.Width, chartPanel.H);
				
				
				// position of ask(BUY) text
				tx = rect.X + 2;
				ty = (float)(chartScale.GetYByValue(curPrc) - (textSize * gtf.Baseline) + ((textSize * gtf.CapsHeight) / 2) - 1);
				
				vec1.X = tx;
				vec1.Y = ty;
				
				if(getAskImbalanceRatio(currProfile.ProfileData, curPrc) >= minRatio)
				{
					askBrush.Opacity = (curPrc == poc || (GetCurrentAsk() == curPrc && curPrc == prc)) ? 1.0f : 0.66f;
					RenderTarget.DrawTextLayout(vec1, tl, askBrush);
				}
				else
				{
					textBrush.Opacity = (curPrc == poc || (GetCurrentAsk() == curPrc && curPrc == prc)) ? 1.0f : 30.55f;	//was1.0f :  0.55f
					RenderTarget.DrawTextLayout(vec1, tl, textBrush);
				}
				
				
				tl.Dispose();
				tf.Dispose();
				
				// ------------------------------------------------------------------------------ //
				
	// bid(SELL) - rect
				
				rect.X      = (float)(rx - (imbWidth * 3) - 2);
				rect.Y      = (float)y1;
				rect.Width  = (float)imbWidth;
				rect.Height = (float)Math.Abs(y1 - y2);
				
//				rectangle color around current POC price
				if(curPrc == poc)
				{
					RenderTarget.DrawRectangle(rect, highBrush);
					RenderTarget.FillRectangle(rect, highBrush);
				}
				else
				{
					RenderTarget.DrawRectangle(rect, cellBrush);
					RenderTarget.FillRectangle(rect, bidBrush);
				}
				
				
//				rectangle color for current bid(buy) price
				if(curPrc == prc)
				{
					highBrush.Opacity = 0.33f;
					RenderTarget.DrawRectangle(rect, highBrush);
					
					rect.Width  = rect.Width  - 1f;
					rect.Height = rect.Height - 1f;
					
					RenderTarget.FillRectangle(rect, highBrush);		// bidBrush
					highBrush.Opacity = 1.0f;
					
					rect.Width  = rect.Width  + 1f;
					rect.Height = rect.Height + 1f;
				}
				
					
////me			//max sell volume	
				if(currProfile.ProfileData[curPrc].BidVolume >= 100)
				{
		//			RenderTarget.DrawRectangle(rect, highestvolumeBrush);
					RenderTarget.FillRectangle(rect, bidBrush);
				}
				
				
	// bid - outline
				
				if(GetCurrentBid() == curPrc && curPrc == prc)
				{
					vec1.X = rect.X - 1;
					vec1.Y = rect.Y;
					
					vec2.X = rect.X + rect.Width;
					vec2.Y = rect.Y;
					
					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
					
					vec1.X = rect.X - 1;
					vec1.Y = rect.Y + rect.Height;
					
					vec2.X = rect.X + rect.Width;
					vec2.Y = rect.Y + rect.Height;
					
					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
				}
				
				// bid - text
				
				fw = (curPrc == poc) ? SharpDX.DirectWrite.FontWeight.UltraBold : SharpDX.DirectWrite.FontWeight.Normal;
				
				tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), fw, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
				
				tf.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				
				tl = new TextLayout(Core.Globals.DirectWriteFactory, currProfile.ProfileData[curPrc].BidVolume.ToString(), tf, rect.Width, chartPanel.H);
				
				tx = rect.X - 3;
				ty = (float)(chartScale.GetYByValue(curPrc) - (textSize * gtf.Baseline) + ((textSize * gtf.CapsHeight) / 2) - 1);
				
				vec1.X = tx;
				vec1.Y = ty;
				
				if(getBidImbalanceRatio(currProfile.ProfileData, curPrc) >= minRatio)
				{
					bidBrush.Opacity = (curPrc == poc || (GetCurrentBid() == curPrc && curPrc == prc)) ? 1.0f : 0.66f;
					RenderTarget.DrawTextLayout(vec1, tl, bidBrush);
				}
				else
				{
					textBrush.Opacity = (curPrc == poc || (GetCurrentBid() == curPrc && curPrc == prc)) ? 1.0f : 30.55f;	//was1.0f :  0.55f
					RenderTarget.DrawTextLayout(vec1, tl, textBrush);
				}
				
				
				tl.Dispose();
				tf.Dispose();
				
				
				// ------------------------------------------------------------------------------ //
				
	// delta ladder - rect
				
				rect.X      = (float)(rx - (imbWidth * 1) - 2);
				rect.Y      = (float)y1;
				rect.Width  = (float)imbWidth;
				rect.Height = (float)Math.Abs(y1 - y2);
				
//					//rectangle color around current POC price
//				if(curPrc == poc)
//				{
//					RenderTarget.DrawRectangle(rect, highBrush);
//					RenderTarget.FillRectangle(rect, highBrush);
//				}
//				else
//				{
//					RenderTarget.DrawRectangle(rect, cellBrush);
//					RenderTarget.FillRectangle(rect, cellBrush);
//				}
				
				
//					//rectangle color for current bid(buy) price
//				if(curPrc == prc)
//				{
//					highBrush.Opacity = 0.33f;
//					RenderTarget.DrawRectangle(rect, highBrush);
					
//					rect.Width  = rect.Width  - 1f;
//					rect.Height = rect.Height - 1f;
					
//					RenderTarget.FillRectangle(rect, highBrush);		// bidBrush
//					highBrush.Opacity = 1.0f;
					
//					rect.Width  = rect.Width  + 1f;
//					rect.Height = rect.Height + 1f;
//				}
				
					
//////me			//max sell volume	
//				if(currProfile.ProfileData[curPrc].BidVolume >= 100)
//				{
//		//			RenderTarget.DrawRectangle(rect, highestvolumeBrush);
//					RenderTarget.FillRectangle(rect, deltaBrush);
//				}
				
				
			
				
				
				
				
	// delta ladder - outline
				
//				if(GetCurrentBid() == curPrc && curPrc == prc)
//				{
//					vec1.X = rect.X - 1;
//					vec1.Y = rect.Y;
					
//					vec2.X = rect.X + rect.Width;
//					vec2.Y = rect.Y;
					
//					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
					
//					vec1.X = rect.X - 1;
//					vec1.Y = rect.Y + rect.Height;
					
//					vec2.X = rect.X + rect.Width;
//					vec2.Y = rect.Y + rect.Height;
					
//					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
//				}
				
				// delta ladder - text
				
				fw = (curPrc == poc) ? SharpDX.DirectWrite.FontWeight.UltraBold : SharpDX.DirectWrite.FontWeight.Normal;
				
				tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), fw, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
				
				tf.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				
				tl = new TextLayout(Core.Globals.DirectWriteFactory, ((currProfile.ProfileData[curPrc].AskVolume) - (currProfile.ProfileData[curPrc].BidVolume)).ToString(), tf, rect.Width, chartPanel.H);
				
				tx = rect.X - 3;
				ty = (float)(chartScale.GetYByValue(curPrc) - (textSize * gtf.Baseline) + ((textSize * gtf.CapsHeight) / 2) - 1);
				
				vec1.X = tx;
				vec1.Y = ty;
				
//				if(getBidImbalanceRatio(currProfile.ProfileData, curPrc) >= minRatio)
				if((currProfile.ProfileData[curPrc].AskVolume) > (currProfile.ProfileData[curPrc].BidVolume))
				{
		//			deltaBrush.Opacity = (curPrc == poc || (GetCurrentBid() == curPrc && curPrc == prc)) ? 1.0f : 0.66f;
		//			RenderTarget.DrawTextLayout(vec1, tl, askBrush);
					RenderTarget.FillRectangle(rect, askBrush);
				}
				if((currProfile.ProfileData[curPrc].AskVolume) < (currProfile.ProfileData[curPrc].BidVolume))
				{
		//			textBrush.Opacity = (curPrc == poc || (GetCurrentBid() == curPrc && curPrc == prc)) ? 1.0f : 30.55f;	//was1.0f :  0.55f
		//			RenderTarget.DrawTextLayout(vec1, tl, bidBrush);
					RenderTarget.FillRectangle(rect, bidBrush);
				}
		// delta text colour				
		//			textBrush.Opacity = (curPrc == poc || (GetCurrentBid() == curPrc && curPrc == prc)) ? 1.0f : 30.55f;	//was1.0f :  0.55f
					RenderTarget.DrawTextLayout(vec1, tl, textBrush);
				// ---
				
				tl.Dispose();
				tf.Dispose();
				
				// ------------------------------------------------------------------------------ //
								
// ------------------------------------------------------------------------------ //
				
	// volume ladder - rect
				
				rect.X      = (float)(rx - (imbWidth * 2) -2);
				rect.Y      = (float)y1;
				rect.Width  = (float)imbWidth;
				rect.Height = (float)Math.Abs(y1 - y2);
				
				
			//		RenderTarget.DrawRectangle(rect, volumeBrush);		//cellBrush);
					RenderTarget.FillRectangle(rect, volumeBrush);		//volumeBrush);		//cellBrush);
				
				
				
				
	//rectangle fill color for total volume on ladder
				if(currProfile.ProfileData[curPrc].TotalVolume == maxVol)
				{
		//			RenderTarget.DrawRectangle(rect, highestvolumeBrush);
					RenderTarget.FillRectangle(rect, highestvolumeBrush);
				}
//				else
//				{
//					RenderTarget.DrawRectangle(rect, volumeBrush);		//cellBrush);
//					RenderTarget.FillRectangle(rect, volumeBrush);		//volumeBrush);		//cellBrush);
//				}
				
//	//rectangle color for current volume price
				
//				if(curPrc == prc)
//				{
//					highBrush.Opacity = 0.33f;
//					RenderTarget.DrawRectangle(rect, volumeBrush);		//highBrush);
					
//					rect.Width  = rect.Width  - 1f;
//					rect.Height = rect.Height - 1f;
					
//					RenderTarget.FillRectangle(rect, highestvolumeBrush);		// bidBrush
//					highBrush.Opacity = 1.0f;
					
//					rect.Width  = rect.Width  + 1f;
//					rect.Height = rect.Height + 1f;
//				}
				
				// volume ladder  - outline
				
//				if(GetCurrentBid() == curPrc && curPrc == prc)
//				{
//					vec1.X = rect.X - 1;
//					vec1.Y = rect.Y;
					
//					vec2.X = rect.X + rect.Width;
//					vec2.Y = rect.Y;
					
//					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
					
//					vec1.X = rect.X - 1;
//					vec1.Y = rect.Y + rect.Height;
					
//					vec2.X = rect.X + rect.Width;
//					vec2.Y = rect.Y + rect.Height;
					
//					RenderTarget.DrawLine(vec1, vec2, currBrush, 1);
//				}
				
				// volume ladder - text
				
				fw = (curPrc == poc) ? SharpDX.DirectWrite.FontWeight.UltraBold : SharpDX.DirectWrite.FontWeight.Normal;
				
				tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), fw, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
				
				tf.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				
				tl = new TextLayout(Core.Globals.DirectWriteFactory, currProfile.ProfileData[curPrc].TotalVolume.ToString(), tf, rect.Width, chartPanel.H);
				
				tx = rect.X ;//- 10;
				ty = (float)(chartScale.GetYByValue(curPrc) - (textSize * gtf.Baseline) + ((textSize * gtf.CapsHeight) / 2) - 1);
				
				vec1.X = tx;
				vec1.Y = ty;
				
//				if(getBidImbalanceRatio(currProfile.ProfileData, curPrc) >= minRatio)
//				{
//					bidBrush.Opacity = (curPrc == poc || (GetCurrentBid() == curPrc && curPrc == prc)) ? 1.0f : 0.66f;
//	//				RenderTarget.DrawTextLayout(vec1, tl, bidBrush);
//				}
//				else
//				{
//					textBrush.Opacity = (curPrc == poc || (GetCurrentBid() == curPrc && curPrc == prc)) ? 1.0f : 30.55f;	//was1.0f :  0.55f
					RenderTarget.DrawTextLayout(vec1, tl, textBrush);
//				}
				
				// ---
				
				tl.Dispose();
				tf.Dispose();
				
				// ------------------------------------------------------------------------------ //
		
				
				
				
				int w = 0;
				
				if(currProfile.ProfileData.ContainsKey(curPrc))
				{
					w = (int)((proWidth / maxVol) * currProfile.ProfileData[curPrc].TotalVolume);
				}
				
				
				// paint horizontal volume histogram
				if(w > 0)
				{
				//	rect.X      = (float)(rx - (imbWidth * 2 + 2) - (w + 2));
					rect.X      = (float)(rx - (imbWidth * 3 + 2) - (w + 2));
					rect.Y      = (float)y1;
					rect.Width  = (float)w;
					rect.Height = (float)Math.Abs(y1 - y2);
					
					//rectangle color around POC total volume 
					if(curPrc == poc)
					{
		//				RenderTarget.DrawRectangle(rect, highBrush);
						RenderTarget.FillRectangle(rect, volumeBrush);	//highestvolumeBrush);		//highBrush);
					}
					else
					{
//		//				RenderTarget.DrawRectangle(rect, cellBrush);
						RenderTarget.FillRectangle(rect, volumeBrush);
					}
					
					//rectangle color around current total volume price
//					if(curPrc == prc)
//					{
//						highBrush.Opacity = 0.33f;
//		//				RenderTarget.DrawRectangle(rect, highBrush);
						
//						rect.Width  = rect.Width  - 1f;
//						rect.Height = rect.Height - 1f;
						
////						RenderTarget.FillRectangle(rect, volumeBrush);		//highBrush);
//						highBrush.Opacity = 1.0f;
						
//						rect.Width  = rect.Width  + 1f;
//						rect.Height = rect.Height + 1f;
//					}
				}
				
				// lines
				
				vec1.X = (float)rect.X - 2f;
				vec1.Y = (float)y1 - 1f;
				vec2.X = (float)rx - 1f;
				vec2.Y = (float)y1 - 1f;
				
				RenderTarget.DrawLine(vec1, vec2, backBrush, 1);
				
				vec1.X = (float)rect.X - 2f;
				vec1.Y = (float)y2 + 1f;
				vec2.X = (float)rx - 1f;
				vec2.Y = (float)y2 + 1f;
				
				RenderTarget.DrawLine(vec1, vec2, backBrush, 1);
				
				vec1.X = (float)rect.X - 1f;
				vec1.Y = (float)y1 - 1f;
				vec2.X = (float)rect.X - 1f;
				vec2.Y = (float)y2 + 1f;
				
				RenderTarget.DrawLine(vec1, vec2, backBrush, 1);
				
				vec1.X = (float)(rx - (imbWidth * 2 + 3));
				vec1.Y = (float)y1 - 1f;
				vec2.X = (float)(rx - (imbWidth * 2 + 3));
				vec2.Y = (float)y2 + 1f;
				
				RenderTarget.DrawLine(vec1, vec2, backBrush, 1);
				
				RenderTarget.DrawLine(vec1, vec2, backBrush, 1);
				
				vec1.X = (float)(rx - (imbWidth + 1));
				vec1.Y = (float)y1 - 1f;
				vec2.X = (float)(rx - (imbWidth + 1));
				vec2.Y = (float)y2 + 1f;
				
				RenderTarget.DrawLine(vec1, vec2, backBrush, 1);
				
				// delta
				
				if(w > 0)
				{
					// delta histogram
					
					if(currProfile.ProfileData[curPrc].TotalVolume == 0.0) { continue; }
					
					int askWidth = (int)((w / currProfile.ProfileData[curPrc].TotalVolume) * currProfile.ProfileData[curPrc].AskVolume);
					int bidWidth = (int)((w / currProfile.ProfileData[curPrc].TotalVolume) * currProfile.ProfileData[curPrc].BidVolume);
					int dtaWidth = Math.Abs(askWidth - bidWidth);
					
					//rectangle color bid(buy) delta volume histogram on top of volume histogram at each price  --- green
					if(dtaWidth > 0 && askWidth > bidWidth)
					{
						rect.X      = (float)(rx - (imbWidth * 3 + 4) - dtaWidth);
						rect.Y      = (float)y1;
						rect.Width  = (float)dtaWidth;
						rect.Height = (float)Math.Abs(y2 - y1);
						
//me	histogram fill colour					
						askBrush.Opacity = (cls == curPrc) ? 0.83f : 0.32f;		//was  ? 0.33f : 0.22f;	
						
//						// determine the bar opacity
//						double curr_percent = 100 * (currProfile.ProfileData[curPrc].AskVolume/currProfile.ProfileData[curPrc].TotalVolume);//(barVolume / maxVolume);
//						double curr_opacity = Math.Round((curr_percent / 100) * 0.8, 1);
//						curr_opacity = curr_opacity == 0 ? 0.1 : curr_opacity;
						
						RenderTarget.DrawRectangle(rect, askBrush);
						
						rect.Width  = rect.Width  - 1f;
						rect.Height = rect.Height - 1f;
						
						RenderTarget.FillRectangle(rect, askBrush);
					}
					
					//rectangle color ask(sell) delta volume histogram on top of volume histogram at each price  --- red
					if(dtaWidth > 0 && bidWidth > askWidth)
					{
						rect.X      = (float)(rx - (imbWidth * 3 + 4) - dtaWidth);
						rect.Y      = (float)y1;
						rect.Width  = (float)Math.Abs(dtaWidth);
						rect.Height = (float)Math.Abs(y2 - y1);

//me	histogram fill colour					
						bidBrush.Opacity = (cls == curPrc) ? 0.83f : 0.32f;		//was  ? 0.33f : 0.22f;		
						
						RenderTarget.DrawRectangle(rect, bidBrush);
						
						rect.Width  = rect.Width  - 1f;
						rect.Height = rect.Height - 1f;
						
						RenderTarget.FillRectangle(rect, bidBrush);
					}
				}
			}
			
//me looking for spread value in ticks
	//		double spread = (CurrentDayOHL().CurrentHigh[0] -  CurrentDayOHL().CurrentLow[0]) * TickSize;

			
			
			// totals at top & bottom of vol profile
			double maxY = currProfile.ProfileData.Keys.Max();
			double minY = currProfile.ProfileData.Keys.Min();
			
			y1 = ((chartScale.GetYByValue(maxY) + chartScale.GetYByValue(maxY + TickSize)) / 2) + 1;
			y2 = ((chartScale.GetYByValue(minY) + chartScale.GetYByValue(minY - TickSize)) / 2) - 1;
			
			tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
			
			tf.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
			
			tl = new TextLayout(Core.Globals.DirectWriteFactory, //" S: "+spread.ToString("n0")+
				"  V: "+currProfile.v.ToString("n0"), tf, chartPanel.W, chartPanel.H);
			
			vec1.X = rx - tl.Metrics.Width;
			vec1.Y = (float)y2 + 3f;
			
//me
			textBrush.Opacity = 30.5f;		// was 0.5f;
			RenderTarget.DrawTextLayout(vec1, tl, textBrush);
			
			// ---
			
			vec1.Y = (float)(y1 - textSize - (textSize * (1.0 - gtf.CapsHeight)) - 3f);
			
			RenderTarget.DrawTextLayout(vec1, tl, textBrush);
			
			// ---
			
			tl = new TextLayout(Core.Globals.DirectWriteFactory, "  D: "+dta.ToString("n0"), tf, chartPanel.W, chartPanel.H);
			
			vec1.X = rx - tl.Metrics.Width;
			vec1.Y = (float)(y2 + 3f + textSize);
			
			if(dta < 0.0)
			{
//me				
				bidBrush.Opacity = 30.4f;		// was 0.4f		
				RenderTarget.DrawTextLayout(vec1, tl, bidBrush);
			}
			else
			{
				askBrush.Opacity = 30.4f;		// was 0.4f
				RenderTarget.DrawTextLayout(vec1, tl, askBrush);
			}
			
			// ---
			
			vec1.Y = (float)(y1 - textSize - (textSize * (1.0 - gtf.CapsHeight)) - 3f - textSize);
			
			if(dta < 0.0)
			{
				bidBrush.Opacity = 90.4f;
				RenderTarget.DrawTextLayout(vec1, tl, bidBrush);
			}
			else
			{
				askBrush.Opacity = 90.4f;
				RenderTarget.DrawTextLayout(vec1, tl, askBrush);
			}
			
			tf.Dispose();
			tl.Dispose();
			
			
			
			//vertical line right side of ladder
					vec1.X = rx+45;		//= rx;
					vec1.Y = y1;
					
					vec2.X = rx+45;		//= rx;
					vec2.Y = y2;
					
					if(dta > 0.0)
					{
						askBrush.Opacity = 0.66f;
						RenderTarget.DrawLine(vec1, vec2, askBrush, 3);		//askBrush, 1);
					}
					if(dta < 0.0)
					{
						bidBrush.Opacity = 0.66f;
						RenderTarget.DrawLine(vec1, vec2, bidBrush, 3);		//bidBrush, 1);
					}
					
			
			// poc
			if(showPoc && currProfile.ProfileData.ContainsKey(poc))
			{
				y1 = ((chartScale.GetYByValue(poc) + chartScale.GetYByValue(poc + TickSize)) / 2);
				y2 = ((chartScale.GetYByValue(poc) + chartScale.GetYByValue(poc - TickSize)) / 2);
				
				rect.X      = (float)(rx - (imbWidth * 2 + 4) - proWidth);
				rect.Y      = (float)y1;
				rect.Width  = (float)proWidth + 1f;
				rect.Height = (float)Math.Abs(y2 - y1);
				
				SharpDX.Direct2D1.LinearGradientBrush gradBrush = new SharpDX.Direct2D1.LinearGradientBrush(RenderTarget, new SharpDX.Direct2D1.LinearGradientBrushProperties()
				{
					StartPoint = new SharpDX.Vector2(rect.X - (proWidth / 2), 0),
					EndPoint   = new SharpDX.Vector2(rect.X + proWidth, 0),
				},
				new SharpDX.Direct2D1.GradientStopCollection(RenderTarget, new SharpDX.Direct2D1.GradientStop[]
				{
					new	SharpDX.Direct2D1.GradientStop()
					{
						Color = (SharpDX.Color)((SharpDX.Direct2D1.SolidColorBrush)pocBrush).Color,
						Position = 0,
					},
					new SharpDX.Direct2D1.GradientStop()
					{
						Color = (SharpDX.Color)((SharpDX.Direct2D1.SolidColorBrush)backBrush).Color,
						Position = 1,
					}
				}));
				
				RenderTarget.DrawRectangle(rect, gradBrush);
				
				gradBrush.Dispose();
			}
			
			// ---
			
			askBrush.Dispose();
			bidBrush.Dispose();
			volumeBrush.Dispose();
			highestvolumeBrush.Dispose();
//			deltaBrush.Dispose();
			
			backBrush.Dispose();
			cellBrush.Dispose();
			highBrush.Dispose();
			currBrush.Dispose();
			textBrush.Dispose();
			
			RenderTarget.AntialiasMode = oldAntialiasMode;		
		}
		#endregion
		
		#region Class Helpers
		
		// getPoc
		//
		private double getPoc(Dictionary<double, RowData> dict)
		{
			double poc = 0.0;
			
			if(dict.Count > 0)
			{
				poc = dict.Keys.Aggregate((i, j) => dict[i].TotalVolume > dict[j].TotalVolume ? i : j);
			}
			
			return poc;
		}
		
		// getDelta
		//
		private double getDelta(Dictionary<double, RowData> dict)
		{
			double askSum = 0.0;
			double bidSum = 0.0;
			
			if(dict.Count > 0)
			{
				foreach(KeyValuePair<double, RowData> rd in dict)
				{
					askSum += rd.Value.AskVolume;
					bidSum += rd.Value.BidVolume;
				}
			}
			
			return (askSum - bidSum);
		}
		
		// getVolume
		//
		private double getVolume(Dictionary<double, RowData> dict, double key)
		{
			double tv = 0.0;
			
			key = Instrument.MasterInstrument.RoundToTickSize(key);
			
			if(dict.ContainsKey(key))
			{
				tv = dict[key].TotalVolume;
			}
			
			return tv;
		}
		
		// getMaxVolume
		//
		private double getMaxVolume(Dictionary<double, RowData> dict)
		{
			double mv = 0.0;
			
			if(dict.Count > 0)
			{
				foreach(KeyValuePair<double, RowData> rd in dict)
				{
					mv = (rd.Value.AskVolume > mv) ? rd.Value.AskVolume : mv;
					mv = (rd.Value.BidVolume > mv) ? rd.Value.BidVolume : mv;
				}
			}
			
			return mv;
		}
		
		// getTotalVolume
		//
		private double getTotalVolume(Dictionary<double, RowData> dict)
		{
			double tv = 0.0;
			
			if(dict.Count > 0)
			{
				foreach(KeyValuePair<double, RowData> rd in dict)
				{
					tv += rd.Value.TotalVolume;
				}
			}
			
			return tv;
		}
		
		// getAskImbalanceRatio
		//
		private double getAskImbalanceRatio(Dictionary<double, RowData> dict, double key)
		{
			double volRatio = 0.0;
			double askPrice = Instrument.MasterInstrument.RoundToTickSize(key);
			double bidPrice = Instrument.MasterInstrument.RoundToTickSize(key - TickSize);
			
			if(!dict.ContainsKey(askPrice) || !dict.ContainsKey(bidPrice))
			{
				return volRatio;
			}
			
			double askVolume = dict[askPrice].AskVolume;
			double bidVolume = dict[bidPrice].BidVolume;
			
			if(askVolume > bidVolume)
			{
				if(askVolume - bidVolume >= minImbalance)
				{
					volRatio = (askVolume - bidVolume) / (askVolume + bidVolume);
				}
			}
			
			return volRatio;
		}
		
		// getBidImbalanceRatio
		//
		private double getBidImbalanceRatio(Dictionary<double, RowData> dict, double key)
		{
			double volRatio = 0.0;
			double askPrice = Instrument.MasterInstrument.RoundToTickSize(key + TickSize);
			double bidPrice = Instrument.MasterInstrument.RoundToTickSize(key);
			
			if(!dict.ContainsKey(askPrice) || !dict.ContainsKey(bidPrice))
			{
				return volRatio;
			}
			
			double askVolume = dict[askPrice].AskVolume;
			double bidVolume = dict[bidPrice].BidVolume;
			
			if(bidVolume > askVolume)
			{
				if(bidVolume - askVolume >= minImbalance)
				{
					volRatio = (bidVolume - askVolume) / (bidVolume + askVolume);
				}
			}
			
			return volRatio;
		}
		
		// getValueArea
		//
		private double[] getValueArea(Dictionary<double, RowData> dict)
		{
			double vah = 0.0;
			double val = 0.0;
			
			double[] ret = {vah,val};
			
			if(dict.Count > 0)
			{
				int    iteCnt = 0;
				double maxPrc = dict.Keys.Max();
				double minPrc = dict.Keys.Min();
				double pocPrc = getPoc(dict);
				double volSum = getTotalVolume(dict);
				double maxVol = volSum * 0.7;
				double volTmp = 0.0;
				double upperP = pocPrc + TickSize;
				double lowerP = pocPrc - TickSize;
				double upperV = 0.0;
				double lowerV = 0.0;
				
				volTmp = getVolume(dict, pocPrc);
				
				while(volTmp < maxVol)
				{
					if((upperP == maxPrc && lowerP == minPrc) || iteCnt >= 300) { break; }
					
					upperV = getVolume(dict, upperP) + getVolume(dict, upperP + TickSize);
					lowerV = getVolume(dict, lowerP) + getVolume(dict, lowerP - TickSize);
					
					if(upperV > lowerV)
					{
						vah	   = Instrument.MasterInstrument.RoundToTickSize(upperP + TickSize);
						volTmp = volTmp + upperV; 
						upperP = Instrument.MasterInstrument.RoundToTickSize(vah + TickSize);
					}
					else
					{
						val	   = Instrument.MasterInstrument.RoundToTickSize(lowerP - TickSize);
						volTmp = volTmp + lowerV; 
						lowerP = Instrument.MasterInstrument.RoundToTickSize(val - TickSize);
					}
					
					iteCnt++;
				}
				
				ret[0] = Math.Min(maxPrc, vah);
				ret[1] = Math.Max(minPrc, val);
			}
			
			return ret;
		}
		
		// fillMissingTicks
		//
//		private void fillMissingTicks(int index)
//		{
//			double hi = Bars.GetHigh(CurrentBar - index);
//			double lo = Bars.GetLow(CurrentBar - index);
//			double pr = Instrument.MasterInstrument.RoundToTickSize(hi);
			
//			while(pr > lo)
//			{
//				if(!BarItems[index].l.ContainsKey(pr))
//				{
//					BarItems[index].l.Add(pr, new RowData());
//				}
				
//				pr = Instrument.MasterInstrument.RoundToTickSize(pr - TickSize);
//			}
//		}
		
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
		
//		private int getCellWidth()
//		{
//			BarItem currBarItem;
			
//			double maxValue = 0.0;
//			float  maxWidth = 0f;
//			float  curWidth = 0f;
			
//			for(int i=ChartBars.FromIndex;i<=ChartBars.ToIndex;i++)
//			{
//				if(BarItems.IsValidDataPointAt(i))
//				{
//					currBarItem = BarItems.GetValueAt(i);
//				}
//				else
//				{
//					continue;
//				}
				
//				if(currBarItem == null) { continue; }
//				if(currBarItem.l.Count == 0) { continue; }
				
//				foreach(KeyValuePair<double, RowData> rd in currBarItem.l)
//				{
//					maxValue = (rd.Value.AskVolume > maxValue) ? rd.Value.AskVolume : maxValue;
//					maxValue = (rd.Value.BidVolume > maxValue) ? rd.Value.BidVolume : maxValue;
//				}	
//			}
			
//			if(maxValue > 0.0)
//			{
//				TextFormat tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
//				TextLayout tl = new TextLayout(Core.Globals.DirectWriteFactory, maxValue.ToString(), tf, ChartPanel.W, ChartPanel.H);
				
//				maxWidth = tl.Metrics.Width;
				
//				tf.Dispose();
//				tl.Dispose();
//			}
			
//			return (int)(maxWidth + 7f);
//		}
		
		#endregion

		#region Properties
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Cell Color", GroupName = "Parameters", Order = 1)]
		public Brush cellColor
		{ get; set; }
		
		[Browsable(false)]
		public string cellColorSerializable
		{
			get { return Serialize.BrushToString(cellColor); }
			set { cellColor = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Highlight Color", GroupName = "Parameters", Order = 2)]
		public Brush highColor
		{ get; set; }
		
		[Browsable(false)]
		public string highColorSerializable
		{
			get { return Serialize.BrushToString(highColor); }
			set { highColor = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Current Close Color", GroupName = "Parameters", Order = 3)]
		public Brush currColor
		{ get; set; }
		
		[Browsable(false)]
		public string currColorSerializable
		{
			get { return Serialize.BrushToString(currColor); }
			set { currColor = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Close Marker Color", GroupName = "Parameters", Order = 4)]
		public Brush markColor
		{ get; set; }
		
		[Browsable(false)]
		public string markColorSerializable
		{
			get { return Serialize.BrushToString(markColor); }
			set { markColor = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[NinjaScriptProperty]
		[Range(0f, 1f)]
		[Display(Name = "Close Marker Opacity", GroupName = "Parameters", Order = 5)]
		public float markOpacity
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Text Color", GroupName = "Parameters", Order = 6)]
		public Brush textColor
		{ get; set; }
		
		[Browsable(false)]
		public string textColorSerializable
		{
			get { return Serialize.BrushToString(textColor); }
			set { textColor = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Text Size", GroupName = "Parameters", Order = 7)]
		public int textSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Ask Color", GroupName = "Parameters", Order = 8)]
		public Brush askColor
		{ get; set; }
		
		[Browsable(false)]
		public string askColorSerializable
		{
			get { return Serialize.BrushToString(askColor); }
			set { askColor = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Bid Color", GroupName = "Parameters", Order = 9)]
		public Brush bidColor
		{ get; set; }
		
		[Browsable(false)]
		public string bidColorSerializable
		{
			get { return Serialize.BrushToString(bidColor); }
			set { bidColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Volume Color", GroupName = "Parameters", Order = 9)]
		public Brush volumeColor
		{ get; set; }
		
		[Browsable(false)]
		public string volumeColorSerializable
		{
			get { return Serialize.BrushToString(volumeColor); }
			set { volumeColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Highest Volume Color", GroupName = "Parameters", Order = 9)]
		public Brush highestvolumeColor
		{ get; set; }
		
		[Browsable(false)]
		public string highestvolumeColorSerializable
		{
			get { return Serialize.BrushToString(highestvolumeColor); }
			set { highestvolumeColor = Serialize.StringToBrush(value); }
		}
		
//		[NinjaScriptProperty]
//		[XmlIgnore]
//		[Display(Name = "Delta Color", GroupName = "Parameters", Order = 9)]
//		public Brush deltaColor
//		{ get; set; }
		
//		[Browsable(false)]
//		public string deltaColorSerializable
//		{
//			get { return Serialize.BrushToString(deltaColor); }
//			set { deltaColor = Serialize.StringToBrush(value); }
//		}
		
		
		
		
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "POC Color", GroupName = "Parameters", Order = 10)]
		public Brush pocColor
		{ get; set; }
		
		[Browsable(false)]
		public string pocColorSerializable
		{
			get { return Serialize.BrushToString(pocColor); }
			set { pocColor = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[NinjaScriptProperty]
		[Range(1.0, double.MaxValue)]
		[Display(Name = "Min. Imbalance Volume", GroupName = "Parameters", Order = 11)]
		public double minImbalance
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Min. Imbalance Ratio", GroupName = "Parameters", Order = 12)]
		public double minRatio
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Auto Scroll", GroupName = "Parameters", Order = 13)]
		public bool autoScroll
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Set Outline Color", GroupName = "Parameters", Order = 14)]
		public bool setOulineColor
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Show Delta", GroupName = "Parameters", Order = 15)]
		public bool showDelta
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Show Bid and Ask", GroupName = "Parameters", Order = 16)]
		public bool showBidAsk
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Show Profile", GroupName = "Parameters", Order = 17)]
		public bool showProfile
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Show POC", GroupName = "Parameters", Order = 18)]
		public bool showPoc
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Show Unfinished Auction", GroupName = "Parameters", Order = 19)]
		public bool showUnfinished
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Fade Cells", GroupName = "Parameters", Order = 20)]
		public bool fadeCells
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Fade Text", GroupName = "Parameters", Order = 21)]
		public bool fadeText
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[ReadOnly(true)]
		[Display(Name = "Indicator Version", GroupName = "Parameters", Order = 22)]
		public string indicatorVersion
		{ get; set; }
		
		
		#region PriceLine
				[NinjaScriptProperty]
				[Display(Name="Show current price", Order=1, GroupName="7. CurrentPrice")]
				public bool ShowCurrentPrice
				{ get; set; }
		
				[XmlIgnore]
				[Display(Name="Spread color", Description="Price SPREAD color", Order=2, GroupName="7. CurrentPrice")]
				public System.Windows.Media.Brush LineSpreadColor		
				{ get; set; }

				[Browsable(false)]
				public string LineSpreadColorSerializable
				{
					get { return Serialize.BrushToString(LineSpreadColor); }
					set { LineSpreadColor = Serialize.StringToBrush(value); }
				}			
				[XmlIgnore]
				[Display(Name="Short color", Description="Price SHORT color", Order=3, GroupName="7. CurrentPrice")]
				public System.Windows.Media.Brush LineShortColor		
				{ get; set; }

				[Browsable(false)]
				public string LineShortColorSerializable
				{
					get { return Serialize.BrushToString(LineShortColor); }
					set { LineShortColor = Serialize.StringToBrush(value); }
				}			

				[XmlIgnore]
				[Display(Name="Long color", Description="Price LONG color", Order=4, GroupName="7. CurrentPrice")]
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

//				[NinjaScriptProperty]
//				[Display(Name="UseExtendedLine", Description="Use Extended Line or measured line", Order=6, GroupName="Parameters")]
//				public bool UseExtendedLine
//				{ get; set; }

//				[Range(1, int.MaxValue)]
//				[NinjaScriptProperty]
//				[Display(Name="RayLength", Description="Line Length when not extended (in number of bars)", Order=7, GroupName="Parameters")]
//				public int RayLength
//				{ get; set; }
				
				[NinjaScriptProperty]
				[Display(Name="ResetProfileOn", Description="Reset Profile On", Order=0, GroupName="General")]
				public Timeframe ResetProfileOn
				{ get; set; }
				
		#endregion
		
		
		#endregion;
		
	}
}

public enum Timeframe { Session, Week, Month, Never };

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ProfileColumns[] cacheProfileColumns;
		public ProfileColumns ProfileColumns(Brush cellColor, Brush highColor, Brush currColor, Brush markColor, float markOpacity, Brush textColor, int textSize, Brush askColor, Brush bidColor, Brush volumeColor, Brush highestvolumeColor, Brush pocColor, double minImbalance, double minRatio, bool autoScroll, bool setOulineColor, bool showDelta, bool showBidAsk, bool showProfile, bool showPoc, bool showUnfinished, bool fadeCells, bool fadeText, string indicatorVersion, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth, Timeframe resetProfileOn)
		{
			return ProfileColumns(Input, cellColor, highColor, currColor, markColor, markOpacity, textColor, textSize, askColor, bidColor, volumeColor, highestvolumeColor, pocColor, minImbalance, minRatio, autoScroll, setOulineColor, showDelta, showBidAsk, showProfile, showPoc, showUnfinished, fadeCells, fadeText, indicatorVersion, showCurrentPrice, lineStyle, lineWidth, resetProfileOn);
		}

		public ProfileColumns ProfileColumns(ISeries<double> input, Brush cellColor, Brush highColor, Brush currColor, Brush markColor, float markOpacity, Brush textColor, int textSize, Brush askColor, Brush bidColor, Brush volumeColor, Brush highestvolumeColor, Brush pocColor, double minImbalance, double minRatio, bool autoScroll, bool setOulineColor, bool showDelta, bool showBidAsk, bool showProfile, bool showPoc, bool showUnfinished, bool fadeCells, bool fadeText, string indicatorVersion, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth, Timeframe resetProfileOn)
		{
			if (cacheProfileColumns != null)
				for (int idx = 0; idx < cacheProfileColumns.Length; idx++)
					if (cacheProfileColumns[idx] != null && cacheProfileColumns[idx].cellColor == cellColor && cacheProfileColumns[idx].highColor == highColor && cacheProfileColumns[idx].currColor == currColor && cacheProfileColumns[idx].markColor == markColor && cacheProfileColumns[idx].markOpacity == markOpacity && cacheProfileColumns[idx].textColor == textColor && cacheProfileColumns[idx].textSize == textSize && cacheProfileColumns[idx].askColor == askColor && cacheProfileColumns[idx].bidColor == bidColor && cacheProfileColumns[idx].volumeColor == volumeColor && cacheProfileColumns[idx].highestvolumeColor == highestvolumeColor && cacheProfileColumns[idx].pocColor == pocColor && cacheProfileColumns[idx].minImbalance == minImbalance && cacheProfileColumns[idx].minRatio == minRatio && cacheProfileColumns[idx].autoScroll == autoScroll && cacheProfileColumns[idx].setOulineColor == setOulineColor && cacheProfileColumns[idx].showDelta == showDelta && cacheProfileColumns[idx].showBidAsk == showBidAsk && cacheProfileColumns[idx].showProfile == showProfile && cacheProfileColumns[idx].showPoc == showPoc && cacheProfileColumns[idx].showUnfinished == showUnfinished && cacheProfileColumns[idx].fadeCells == fadeCells && cacheProfileColumns[idx].fadeText == fadeText && cacheProfileColumns[idx].indicatorVersion == indicatorVersion && cacheProfileColumns[idx].ShowCurrentPrice == showCurrentPrice && cacheProfileColumns[idx].LineStyle == lineStyle && cacheProfileColumns[idx].LineWidth == lineWidth && cacheProfileColumns[idx].ResetProfileOn == resetProfileOn && cacheProfileColumns[idx].EqualsInput(input))
						return cacheProfileColumns[idx];
			return CacheIndicator<ProfileColumns>(new ProfileColumns(){ cellColor = cellColor, highColor = highColor, currColor = currColor, markColor = markColor, markOpacity = markOpacity, textColor = textColor, textSize = textSize, askColor = askColor, bidColor = bidColor, volumeColor = volumeColor, highestvolumeColor = highestvolumeColor, pocColor = pocColor, minImbalance = minImbalance, minRatio = minRatio, autoScroll = autoScroll, setOulineColor = setOulineColor, showDelta = showDelta, showBidAsk = showBidAsk, showProfile = showProfile, showPoc = showPoc, showUnfinished = showUnfinished, fadeCells = fadeCells, fadeText = fadeText, indicatorVersion = indicatorVersion, ShowCurrentPrice = showCurrentPrice, LineStyle = lineStyle, LineWidth = lineWidth, ResetProfileOn = resetProfileOn }, input, ref cacheProfileColumns);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ProfileColumns ProfileColumns(Brush cellColor, Brush highColor, Brush currColor, Brush markColor, float markOpacity, Brush textColor, int textSize, Brush askColor, Brush bidColor, Brush volumeColor, Brush highestvolumeColor, Brush pocColor, double minImbalance, double minRatio, bool autoScroll, bool setOulineColor, bool showDelta, bool showBidAsk, bool showProfile, bool showPoc, bool showUnfinished, bool fadeCells, bool fadeText, string indicatorVersion, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth, Timeframe resetProfileOn)
		{
			return indicator.ProfileColumns(Input, cellColor, highColor, currColor, markColor, markOpacity, textColor, textSize, askColor, bidColor, volumeColor, highestvolumeColor, pocColor, minImbalance, minRatio, autoScroll, setOulineColor, showDelta, showBidAsk, showProfile, showPoc, showUnfinished, fadeCells, fadeText, indicatorVersion, showCurrentPrice, lineStyle, lineWidth, resetProfileOn);
		}

		public Indicators.ProfileColumns ProfileColumns(ISeries<double> input , Brush cellColor, Brush highColor, Brush currColor, Brush markColor, float markOpacity, Brush textColor, int textSize, Brush askColor, Brush bidColor, Brush volumeColor, Brush highestvolumeColor, Brush pocColor, double minImbalance, double minRatio, bool autoScroll, bool setOulineColor, bool showDelta, bool showBidAsk, bool showProfile, bool showPoc, bool showUnfinished, bool fadeCells, bool fadeText, string indicatorVersion, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth, Timeframe resetProfileOn)
		{
			return indicator.ProfileColumns(input, cellColor, highColor, currColor, markColor, markOpacity, textColor, textSize, askColor, bidColor, volumeColor, highestvolumeColor, pocColor, minImbalance, minRatio, autoScroll, setOulineColor, showDelta, showBidAsk, showProfile, showPoc, showUnfinished, fadeCells, fadeText, indicatorVersion, showCurrentPrice, lineStyle, lineWidth, resetProfileOn);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ProfileColumns ProfileColumns(Brush cellColor, Brush highColor, Brush currColor, Brush markColor, float markOpacity, Brush textColor, int textSize, Brush askColor, Brush bidColor, Brush volumeColor, Brush highestvolumeColor, Brush pocColor, double minImbalance, double minRatio, bool autoScroll, bool setOulineColor, bool showDelta, bool showBidAsk, bool showProfile, bool showPoc, bool showUnfinished, bool fadeCells, bool fadeText, string indicatorVersion, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth, Timeframe resetProfileOn)
		{
			return indicator.ProfileColumns(Input, cellColor, highColor, currColor, markColor, markOpacity, textColor, textSize, askColor, bidColor, volumeColor, highestvolumeColor, pocColor, minImbalance, minRatio, autoScroll, setOulineColor, showDelta, showBidAsk, showProfile, showPoc, showUnfinished, fadeCells, fadeText, indicatorVersion, showCurrentPrice, lineStyle, lineWidth, resetProfileOn);
		}

		public Indicators.ProfileColumns ProfileColumns(ISeries<double> input , Brush cellColor, Brush highColor, Brush currColor, Brush markColor, float markOpacity, Brush textColor, int textSize, Brush askColor, Brush bidColor, Brush volumeColor, Brush highestvolumeColor, Brush pocColor, double minImbalance, double minRatio, bool autoScroll, bool setOulineColor, bool showDelta, bool showBidAsk, bool showProfile, bool showPoc, bool showUnfinished, bool fadeCells, bool fadeText, string indicatorVersion, bool showCurrentPrice, DashStyleHelper lineStyle, int lineWidth, Timeframe resetProfileOn)
		{
			return indicator.ProfileColumns(input, cellColor, highColor, currColor, markColor, markOpacity, textColor, textSize, askColor, bidColor, volumeColor, highestvolumeColor, pocColor, minImbalance, minRatio, autoScroll, setOulineColor, showDelta, showBidAsk, showProfile, showPoc, showUnfinished, fadeCells, fadeText, indicatorVersion, showCurrentPrice, lineStyle, lineWidth, resetProfileOn);
		}
	}
}

#endregion
