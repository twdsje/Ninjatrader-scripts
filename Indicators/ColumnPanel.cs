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
	public class ColumnPanel : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "ColumnPanel";
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
				
				textSize			= 11;
				LeftJustified = false;
				Resizable = false;
				IsInitalized = false;
			}
			else if (State == State.Historical)
			{
				if (ChartControl == null) return;
				
				InitalizeColumnPanel(ChartControl);
				InitializeLifetimeDrawingTools();
			}
			else if (State == State.Terminated)
            {
                if (ChartControl == null) return;
                
				TerminateColumnPanel(ChartControl);
            }
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
		
		public void InitalizeColumnPanel(ChartControl chartControl)
		{
			MinimumWidth = CalculateMinimumWidth();
			MinimumTextHeight = CalculateMinimumTextHeight();
			CurrentWidth = MinimumWidth;
			CalculatePosition(chartControl);
			
			if(LeftJustified)
			{
				//chartControl.Properties.BarMarginLeft += CurrentWidth;
			}
			else
			{
            	chartControl.Properties.BarMarginRight += CurrentWidth;
			}
			
			IsInitalized = true;
		}
		
		public void TerminateColumnPanel(ChartControl chartControl)
		{
		    if (ChartControl == null || IsInitalized != true) return;
			
			if(LeftJustified)
			{
				//chartControl.Properties.BarMarginLeft -= CurrentWidth;
			}
			else
			{
            	chartControl.Properties.BarMarginRight -= CurrentWidth;
			}
		}
		
		public void InitializeLifetimeDrawingTools()
		{		
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, textSize)
            {
                TextAlignment = TextAlignment.Trailing,   //TextAlignment.Leading,
                WordWrapping = WordWrapping.NoWrap
            };
		}
		
		public void OnWidthChanged()
		{
			
		}
		
		public void CalculatePosition(ChartControl chartControl)
		{
			if(LeftJustified == true)
			{
				Position = chartControl.CanvasLeft;
			}
			else
			{
				Position = chartControl.CanvasRight;
			}
			
			//Adjust our position based on the width of all indicators on the same side of the chart that come before us.
			foreach (NinjaTrader.Gui.NinjaScript.IndicatorRenderBase indicator in chartControl.Indicators)
			{
				if(indicator == this)
				{
					return;
				}
				else if(indicator is ColumnPanel && indicator.State != State.SetDefaults)
				{
					ColumnPanel currentpanel = indicator as ColumnPanel;
					
					if(currentpanel.LeftJustified == LeftJustified)
					{
						Position -= JustificationFactor * currentpanel.CurrentWidth;
					}
				}
			}
		}
		
		public int CalculateMinimumWidth()
		{
			return getTextWidth("99000") + 6;
		}
		
		public int CalculateMinimumTextHeight()
		{
			return getTextHeight("0") - 5;
		}
		
		private int getTextWidth(string text)
		{
			SimpleFont sf = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", textSize);
			
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
		
		#region Properties
	
		public int MinimumWidth
		{ get; set; }
		
		public int CurrentWidth
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Left Justified", GroupName = "Column Style")]
		public bool LeftJustified
		{ get; set; }
		
		private int JustificationFactor
		{
			get
			{
				return (LeftJustified == false ? 1 : -1);
			}
		}
		
		public bool Resizable
		{ get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Text Size", GroupName = "Parameters", Order = 7)]
		public int textSize
		{ get; set; }
		
		public TextFormat textFormat
		{ get; set; }
		
		[Browsable(false)]
		public int Position
		{ get; set; }
		
		[Browsable(false)]
		public bool IsInitalized
		{ get; set; }
		
		[Browsable(false)]
		public int MinimumTextHeight
		{ get; set; }
		
		#endregion
	}
	

}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ColumnPanel[] cacheColumnPanel;
		public ColumnPanel ColumnPanel(bool leftJustified, int textSize)
		{
			return ColumnPanel(Input, leftJustified, textSize);
		}

		public ColumnPanel ColumnPanel(ISeries<double> input, bool leftJustified, int textSize)
		{
			if (cacheColumnPanel != null)
				for (int idx = 0; idx < cacheColumnPanel.Length; idx++)
					if (cacheColumnPanel[idx] != null && cacheColumnPanel[idx].LeftJustified == leftJustified && cacheColumnPanel[idx].textSize == textSize && cacheColumnPanel[idx].EqualsInput(input))
						return cacheColumnPanel[idx];
			return CacheIndicator<ColumnPanel>(new ColumnPanel(){ LeftJustified = leftJustified, textSize = textSize }, input, ref cacheColumnPanel);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ColumnPanel ColumnPanel(bool leftJustified, int textSize)
		{
			return indicator.ColumnPanel(Input, leftJustified, textSize);
		}

		public Indicators.ColumnPanel ColumnPanel(ISeries<double> input , bool leftJustified, int textSize)
		{
			return indicator.ColumnPanel(input, leftJustified, textSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ColumnPanel ColumnPanel(bool leftJustified, int textSize)
		{
			return indicator.ColumnPanel(Input, leftJustified, textSize);
		}

		public Indicators.ColumnPanel ColumnPanel(ISeries<double> input , bool leftJustified, int textSize)
		{
			return indicator.ColumnPanel(input, leftJustified, textSize);
		}
	}
}

#endregion
