#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
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
			
	public class VolumeProfileColumn : ColumnPanel
	{	
		public class RowData
		{
			public long TotalVolume;
			public long AskVolume;
			public long BidVolume;
			public long DeltaVolume;
			public double Price;
			
			public RowData(long totalVolume, long askVolume, long bidVolume, double price)
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
			public Dictionary<double, RowData> Data;
			public long HiValue;
			public double HiPrice;

			public Profile()
			{
				Data = new Dictionary<double, RowData>();				
			}
			
			public void AddAskVolume(double price, long volume)
			{
				RowData val;
				
				if (Data.TryGetValue(price, out val))
			    {
			        val.AskVolume += volume;
					val.TotalVolume += volume;	
			    }
			    else
			    {
			        Data.Add(price, new RowData(volume, volume, 0, price));
			    }		
				
				CalculateValues();
			}
			
			public void AddBidVolume(double price, long volume)
			{
				RowData val;
				
				if (Data.TryGetValue(price, out val))
			    {
					val.BidVolume += volume;
					val.TotalVolume += volume;
				}
				else
				{
					Data.Add(price, new RowData(volume, 0, volume, price));
			    }
				
				CalculateValues();
			}
			
			public void CalculateValues()
			{
				HiValue = 0;
				
				foreach(KeyValuePair<double, RowData> kvp in Data)
				{
					RowData r = kvp.Value;
					
					if(Math.Abs(r.TotalVolume) > HiValue)
					{
						HiValue = Math.Abs(r.TotalVolume);
						HiPrice = r.Price;
					}
				}
			}
		}		
		
		#region Private Variables
		

		private Profile myProfile = new Profile();		
		private SessionIterator sessionIterator;
		
		#endregion
		
		protected override void OnStateChange()
		{
			base.OnStateChange();
			
			if (State == State.SetDefaults)
			{				
				Description					= "Shows volume traded at each price for specified time period.";
				Name						= "Volume Profile Column";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= true;
				IsAutoScale 				= false;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= false;
				BarsRequiredToPlot			= 2;
				ScaleJustification			= ScaleJustification.Right;
				
				positiveColor    	 	= Brushes.CornflowerBlue;
				negativeColor    	 	= Brushes.Orange;
				neutralColor    	 	= Brushes.White;
				textSize			= 11;
				ResizableWidth = 30;
			}
			else if (State == State.Configure)
			{							
				ZOrder = ChartBars.ZOrder - 1;
				
				if(!Bars.IsTickReplay)
				{
					Draw.TextFixed(this, "tickReplay", "Please enable Tick Replay!", TextPosition.TopRight);
				}				
				
				//AddDataSeries(BarsPeriodType.Day, 1);
				sessionIterator = new SessionIterator(Bars);				
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
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{	
			base.OnRender(chartControl, chartScale);	
			if(Bars == null || Bars.Instrument == null || CurrentBar < 1) { return; }
			
			LastRender = DateTime.Now;
			
			try
        	{
				
				foreach(KeyValuePair<double, RowData> row in myProfile.Data)
				{
					drawRow(chartControl, chartScale, row.Value);
				}
				
			}
			catch(Exception e)
	        {

	            Print("Volume Profile Column: " + e.Message);
	        }
		}
		
		private void drawRow(ChartControl chartControl, ChartScale chartScale, RowData row)
		{
			
			//Calculate color of this row.
			//Brush brushColor	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0)); //bidColor.Freeze();		
			float alpha = alpha = (float)((double)Math.Abs(row.TotalVolume) / (double)myProfile.HiValue);
			Brush brushColor = neutralColor;			
			
			//Calculate width of this row.
			
			
				
			//Calculate cell properties
			double y1 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price + TickSize)) / 2) + 1;
			double y2 = ((chartScale.GetYByValue(row.Price) + chartScale.GetYByValue(row.Price - TickSize)) / 2) - 1;
			
			SharpDX.RectangleF rect = new SharpDX.RectangleF();
			rect.X      = (float)chartControl.CanvasRight - Position;
			rect.Y      = (float)y1;
			rect.Width  = (float)-((ResizableWidth * alpha) + MinimumWidth - 2);
			rect.Height = (float)Math.Abs(y1 - y2);			
			
			//Draw the row.
			using(SharpDX.Direct2D1.Brush rowBrush =  brushColor.ToDxBrush(RenderTarget))
			{
				//rowBrush.Opacity = alpha;
				rowBrush.Opacity = alpha;
				RenderTarget.FillRectangle(rect, neutralColor.ToDxBrush(RenderTarget));
				RenderTarget.FillRectangle(rect, rowBrush);
			}

			if(rect.Height > this.MinimumTextHeight)
			{
				RenderTarget.DrawText(string.Format("{0}", row.TotalVolume), textFormat, rect, TextColor.ToDxBrush(RenderTarget));
			}
		}
		
		#region Properties
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name="ResetProfileOn", Description="Reset Profile On", Order=0, GroupName="General")]
		public Timeframe ResetProfileOn
		{ get; set; }
		
		[XmlIgnore]
		[Browsable(false)]
		public string ResetProfileOnSerializable
		{
			get { return ResetProfileOn.ToString(); }
			set { ResetProfileOn = (Timeframe) Enum.Parse(typeof(Timeframe), value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Negative Delta Color", GroupName = "Parameters", Order = 9)]
		public Brush negativeColor
		{ get; set; }
		
		[Browsable(false)]
		public string negativeColorSerializable
		{
			get { return Serialize.BrushToString(negativeColor); }
			set { negativeColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Positive Delta Color", GroupName = "Parameters", Order = 9)]
		public Brush positiveColor
		{ get; set; }
		
		[Browsable(false)]
		public string positiveColorSerializable
		{
			get { return Serialize.BrushToString(positiveColor); }
			set { positiveColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Neutral Delta Color", GroupName = "Parameters", Order = 9)]
		public Brush neutralColor
		{ get; set; }
		
		[Browsable(false)]
		public string neutralColorSerializable
		{
			get { return Serialize.BrushToString(neutralColor); }
			set { neutralColor = Serialize.StringToBrush(value); }
		}
				
		#endregion
	}
}


























































































































