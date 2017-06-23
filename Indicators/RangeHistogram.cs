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
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class RangeHistogram : Indicator
	{
		public Dictionary<double, int> Profile = new Dictionary<double, int>();
		
		public int Max = 0;
		
		private bool setScale = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"A histogram showing the distribution and standard distribution of bar ranges.";
				Name										= "RangeHistogram";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				BarsRequiredToPlot			= 0;
				
				Deviation01	 = 1;
				NormalColor  = Brushes.White;
				OutlierColor = Brushes.CornflowerBlue;
				
				textSize			= 11;
				TextColor = Brushes.Black;
			}
			else if (State == State.Historical)
			{
				if (ChartControl == null) return;
				
				InitializeLifetimeDrawingTools();
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			double rangeInTicks = Range()[0] / TickSize;
			
			if (Profile.ContainsKey(rangeInTicks))
		    {
                Profile[rangeInTicks]++;
		    }
		    else
		    {
		        Profile.Add(rangeInTicks, 1);
				setScale = true;
		    }	

			Max = Profile.Values.Max();
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{	
			base.OnRender(chartControl, chartScale);				
			if(Bars == null || Bars.Instrument == null || IsInHitTest || CurrentBar < 1) { return; }
				
			foreach(KeyValuePair<double, int> row in Profile)
			{
				drawRow(chartControl, chartScale, row.Key, row.Value);
			}
		}
		
		private void drawRow(ChartControl chartControl, ChartScale chartScale, double value, int quantity)
		{
			//Calculate color of this row.
			//Brush brushColor	= new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0)); //bidColor.Freeze();		
			//float alpha = alpha = (float)((double)Math.Abs(row.TotalVolume) / (double)myProfile.HiValue);
			Brush brushColor = NormalColor;			
			
				
			//Calculate cell properties
			double y1 = ((chartScale.GetYByValue(value) + chartScale.GetYByValue(value + 1)) / 2) + 1;
			double y2 = ((chartScale.GetYByValue(value) + chartScale.GetYByValue(value - 1)) / 2) - 1;
			
			SharpDX.RectangleF rect = new SharpDX.RectangleF();
			rect.X      = (float)chartControl.CanvasRight;
			rect.Y      = (float)y1;
			rect.Width  = (float)((chartControl.CanvasLeft - chartControl.CanvasRight) * Math.Log(quantity) / Math.Log(Max));
			rect.Height = (float)Math.Abs(y1 - y2);			

			//Draw the row.
			using(SharpDX.Direct2D1.Brush rowBrush =  brushColor.ToDxBrush(RenderTarget))
			{
				RenderTarget.FillRectangle(rect, NormalColor.ToDxBrush(RenderTarget));
				//RenderTarget.FillRectangle(rect, rowBrush);
			}
			
//			if(rect.Height > this.MinimumTextHeight)
//			{
//				RenderTarget.DrawText(string.Format("{0}", quantity), textFormat, rect, TextColor.ToDxBrush(RenderTarget));
//			}
		}
		
		public override void OnCalculateMinMax()
		{
			MinValue = Profile.Keys.Min();
			MaxValue = Profile.Keys.Max();
		}
		
		public int CalculateMinimumTextHeight()
		{
			return getTextHeight("0") - 5;
		}
		
		private int getTextHeight(string text)
		{
			SimpleFont sf = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", textSize);
			
			float textHeight = 0f;
			
			if(text.Length > 0)
			{
				TextFormat tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
				TextLayout tl = new TextLayout(Core.Globals.DirectWriteFactory, text, tf, ChartPanel.W, ChartPanel.H);
				
				textHeight = tl.Metrics.Height;
				
				tf.Dispose();
				tl.Dispose();
			}
			
			return (int)textHeight;
		}
		
		public void InitializeLifetimeDrawingTools()
		{		
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, textSize)
            {
                TextAlignment = TextAlignment.Trailing,   //TextAlignment.Leading,
                WordWrapping = WordWrapping.NoWrap
            };
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Deviation01", Description="Values within this range from the mode are considered normal.", Order=1, GroupName="Parameters")]
		public int Deviation01
		{ get; set; }
		#endregion
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Normal values color", GroupName = "Parameters", Order = 9)]
		public Brush NormalColor
		{ get; set; }
		
		[Browsable(false)]
		public string normalColorSerializable
		{
			get { return Serialize.BrushToString(NormalColor); }
			set { NormalColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Outlier values Color", GroupName = "Parameters", Order = 9)]
		public Brush OutlierColor
		{ get; set; }
		
		[Browsable(false)]
		public string outlierColorSerializable
		{
			get { return Serialize.BrushToString(OutlierColor); }
			set { OutlierColor = Serialize.StringToBrush(value); }
		}
		
		protected TextFormat textFormat
		{ get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Text Size", GroupName = "Parameters", Order = 7)]
		public int textSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Text Color", GroupName = "Parameters", Order = 9)]
		public Brush TextColor
		{ get; set; }
		
		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}
		
		protected int MinimumTextHeight
		{ get; set; }
		
		

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RangeHistogram[] cacheRangeHistogram;
		public RangeHistogram RangeHistogram(int deviation01, Brush normalColor, Brush outlierColor, int textSize, Brush textColor)
		{
			return RangeHistogram(Input, deviation01, normalColor, outlierColor, textSize, textColor);
		}

		public RangeHistogram RangeHistogram(ISeries<double> input, int deviation01, Brush normalColor, Brush outlierColor, int textSize, Brush textColor)
		{
			if (cacheRangeHistogram != null)
				for (int idx = 0; idx < cacheRangeHistogram.Length; idx++)
					if (cacheRangeHistogram[idx] != null && cacheRangeHistogram[idx].Deviation01 == deviation01 && cacheRangeHistogram[idx].NormalColor == normalColor && cacheRangeHistogram[idx].OutlierColor == outlierColor && cacheRangeHistogram[idx].textSize == textSize && cacheRangeHistogram[idx].TextColor == textColor && cacheRangeHistogram[idx].EqualsInput(input))
						return cacheRangeHistogram[idx];
			return CacheIndicator<RangeHistogram>(new RangeHistogram(){ Deviation01 = deviation01, NormalColor = normalColor, OutlierColor = outlierColor, textSize = textSize, TextColor = textColor }, input, ref cacheRangeHistogram);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RangeHistogram RangeHistogram(int deviation01, Brush normalColor, Brush outlierColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogram(Input, deviation01, normalColor, outlierColor, textSize, textColor);
		}

		public Indicators.RangeHistogram RangeHistogram(ISeries<double> input , int deviation01, Brush normalColor, Brush outlierColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogram(input, deviation01, normalColor, outlierColor, textSize, textColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RangeHistogram RangeHistogram(int deviation01, Brush normalColor, Brush outlierColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogram(Input, deviation01, normalColor, outlierColor, textSize, textColor);
		}

		public Indicators.RangeHistogram RangeHistogram(ISeries<double> input , int deviation01, Brush normalColor, Brush outlierColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogram(input, deviation01, normalColor, outlierColor, textSize, textColor);
		}
	}
}

#endregion
