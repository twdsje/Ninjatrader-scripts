//Originally created by Seth Ellis twdsje@gmail.com

#region Using declarations
using System;

using System.Windows;
using System.Windows.Input;

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
using SharpDX.DirectWrite;

#endregion

//This namespace holds Drawing tools in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.DrawingTools
{
	public class irArrowLine : ArrowLine
	{
		private TextFormat cachedTextFormat;
		private	Gui.Tools.SimpleFont font;
		private bool Initalized;
		
		protected override void OnStateChange()
		{
			base.OnStateChange();
			
			if (State == State.SetDefaults)
			{
				Description		= @"An arrow line with a price marker attached.";
				Name			= "irArrowLine";
				
				Font			= new Gui.Tools.SimpleFont() { Size = 14 };	
				TextColor 		= Stroke.Brush;
				Text 			= "";
			}
			else if (State == State.Configure)
			{
			}
		}
		
		public void InitializeDrawingTools()
		{	
			if(Initalized) return;
			
			cachedTextFormat = Font.ToDirectWriteTextFormat();
		}

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			
			InitializeDrawingTools();
			
			//Calculate where to draw the text.
			//Refer to parent class Lines.cs OnRender method for more information.
			ChartPanel	panel			= chartControl.ChartPanels[chartScale.PanelIndex];
			double		strokePixAdj	=	((double)(Stroke.Width % 2)).ApproxCompare(0) == 0 ? 0.5d : 0d;
			Vector		pixelAdjustVec	= new Vector(strokePixAdj, strokePixAdj);
			
			Point					endPoint			= EndAnchor.GetPoint(chartControl, panel, chartScale);
			Point					endPointAdjusted	= endPoint + pixelAdjustVec;
			SharpDX.Vector2			endVec				= endPointAdjusted.ToVector2();
			
			//Calculate the current price.			
			string text = (Text != "" ? Text : chartControl.Instrument.MasterInstrument.FormatPrice(chartScale.GetValueByY(endVec.Y), true));
			
			//Set properties for rectangle to draw the object.			
			TextLayout tl = new TextLayout(Core.Globals.DirectWriteFactory, text, cachedTextFormat, ChartPanel.W, ChartPanel.H);
			SharpDX.RectangleF rect = new SharpDX.RectangleF();
			rect.X      = (float)endVec.X;
			rect.Y      = (float)endVec.Y - 6f;
			rect.Width  = (float)-(tl.Metrics.Width);
			rect.Height = (float)-(tl.Metrics.Height);		
			
			//Draw the text.
			using(SharpDX.Direct2D1.Brush myDxBrush =  TextColor.ToDxBrush(RenderTarget))
			{
				chartScale.GetValueByY(endVec.Y);RenderTarget.DrawText(string.Format("{0}", text), cachedTextFormat, rect, myDxBrush);
			}
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Text", GroupName = "Label")]
		public string Text
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolTextFont", GroupName = "Label")]
		public Gui.Tools.SimpleFont Font
		{
			get { return font; }
			set
			{
				font				= value;
				Initalized	= false;
			}
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Text Color", GroupName = "Label")]
		public Brush TextColor
		{ get; set; }
		
		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}

	}
}
