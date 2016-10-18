// 
// Copyright (C) 2015, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using NinjaTrader.Data;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.NinjaScript.SuperDomColumns;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

namespace NinjaTrader.NinjaScript.SuperDomColumns
{
	
	public enum VolumeSide {bid, ask};
	
	public class CurrentTrades : SuperDomColumn
	{
		private readonly	object			barsSync				= new object();
		private				bool			clearLoadingSent;
		private				FontFamily		fontFamily;
		private				FontStyle		fontStyle;
		private				FontWeight		fontWeight;
		private				Pen				gridPen;			
		private				double			halfPenWidth;
		private				bool			heightUpdateNeeded;
		private				int				lastMaxIndex			= -1;
		private				long			maxVolume;
		private				bool			mouseEventsSubscribed;
		private				double			textHeight;
		private				Point			textPosition			= new Point(4, 0);
		private				string			tradingHoursData		= TradingHours.UseInstrumentSettings;
		private				long			totalBuyVolume;
		private				long			totalLastVolume;
		private				long			totalSellVolume;
		private				Typeface		typeFace;
		private				double			lastBid = 0;
		private				double			lastAsk = 0;
		private				double			lastClose = 0;


		private void OnBarsUpdate(object sender, BarsUpdateEventArgs e)
		{
			if (State == State.Active && SuperDom != null && SuperDom.IsConnected)
			{
				if (SuperDom.IsReloading)
				{
					OnPropertyChanged();
					return;
				}

				BarsUpdateEventArgs barsUpdate = e;
				lock (barsSync)
				{
					int currentMaxIndex = barsUpdate.MaxIndex;					
					
					for (int i = lastMaxIndex + 1; i <= currentMaxIndex; i++)
					{
						
						if (barsUpdate.BarsSeries.GetIsFirstBarOfSession(i))
						{
							// If a new session starts, clear out the old values and start fresh
							maxVolume		= 0;
							totalBuyVolume	= 0;
							totalLastVolume	= 0;
							totalSellVolume	= 0;
							Sells.Clear();
							Buys.Clear();
							LastVolumes.Clear();
						}

						double	ask		= barsUpdate.BarsSeries.GetAsk(i);
						double	bid		= barsUpdate.BarsSeries.GetBid(i);
						double	close	= barsUpdate.BarsSeries.GetClose(i);
						long	volume	= barsUpdate.BarsSeries.GetVolume(i);

						if (ask != double.MinValue && close >= ask)
						{
							Buys.AddOrUpdate(close, volume, (price, oldVolume) => oldVolume + volume);
							totalBuyVolume += volume;
						}
						else if (bid != double.MinValue && close <= bid)
						{
							Sells.AddOrUpdate(close, volume, (price, oldVolume) => oldVolume + volume);
							totalSellVolume += volume;
						}

						
						if((Bidask == VolumeSide.bid && lastBid != bid && close <= bid) || (Bidask == VolumeSide.ask && lastAsk != ask && close >= ask))
						//if(lastBid != bid && lastAsk != ask && lastClose != close)
						//if(lastClose != close)
						{
							lastBid = bid;
							lastAsk = ask;
							lastClose = close;
							
							long newVolume;
							LastVolumes.AddOrUpdate(close, newVolume = volume, (price, oldVolume) => newVolume = 0);
							totalLastVolume += volume;

							if (newVolume > maxVolume)
								maxVolume = newVolume;
						}
						
						if ((Bidask == VolumeSide.bid && close <= bid) || (Bidask == VolumeSide.ask && close >= ask))
						{
							long newVolume;
							LastVolumes.AddOrUpdate(close, newVolume = volume, (price, oldVolume) => newVolume = oldVolume + volume);
							totalLastVolume += volume;

							if (newVolume > maxVolume)
								maxVolume = newVolume;
						}
					}

					lastMaxIndex = barsUpdate.MaxIndex;
					if (!clearLoadingSent)
					{
						SuperDom.Dispatcher.InvokeAsync(() => SuperDom.ClearLoadingString());
						clearLoadingSent = true;
					}
				}
			}
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			OnPropertyChanged();
		}

		private void OnMouseEnter(object sender, MouseEventArgs e)
		{
			OnPropertyChanged();
		}

		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			OnPropertyChanged();
		}

		protected override void OnRender(DrawingContext dc, double renderWidth)
		{
			// This may be true if the UI for a column hasn't been loaded yet (e.g., restoring multiple tabs from workspace won't load each tab until it's clicked by the user)
			if (gridPen == null)
			{
				if (UiWrapper != null && PresentationSource.FromVisual(UiWrapper) != null)
				{
					Matrix m			= PresentationSource.FromVisual(UiWrapper).CompositionTarget.TransformToDevice;
					double dpiFactor	= 1 / m.M11;
					gridPen				= new Pen(Application.Current.TryFindResource("BorderThinBrush") as Brush, 1 * dpiFactor);
					halfPenWidth		= gridPen.Thickness * 0.5;
				}
			}

			if (fontFamily != SuperDom.Font.Family
				|| (SuperDom.Font.Italic && fontStyle != FontStyles.Italic)
				|| (!SuperDom.Font.Italic && fontStyle == FontStyles.Italic)
				|| (SuperDom.Font.Bold && fontWeight != FontWeights.Bold)
				|| (!SuperDom.Font.Bold && fontWeight == FontWeights.Bold))
			{
				// Only update this if something has changed
				fontFamily	= SuperDom.Font.Family;
				fontStyle	= SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal;
				fontWeight	= SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal;
				typeFace	= new Typeface(fontFamily, fontStyle, fontWeight, FontStretches.Normal);
				heightUpdateNeeded = true;
			}

			double	verticalOffset	= -gridPen.Thickness;

			lock (SuperDom.Rows)
				foreach (PriceRow row in SuperDom.Rows)
				{
					if (renderWidth - halfPenWidth >= 0)
					{
						// Draw cell
						Rect rect = new Rect(-halfPenWidth, verticalOffset, renderWidth - halfPenWidth, SuperDom.ActualRowHeight);

						// Create a guidelines set
						GuidelineSet guidelines = new GuidelineSet();
						guidelines.GuidelinesX.Add(rect.Left	+ halfPenWidth);
						guidelines.GuidelinesX.Add(rect.Right	+ halfPenWidth);
						guidelines.GuidelinesY.Add(rect.Top		+ halfPenWidth);
						guidelines.GuidelinesY.Add(rect.Bottom	+ halfPenWidth);

						dc.PushGuidelineSet(guidelines);
						dc.DrawRectangle(BackColor, null, rect);
						dc.DrawLine(gridPen, new Point(-gridPen.Thickness, rect.Bottom), new Point(renderWidth - halfPenWidth, rect.Bottom));
						dc.DrawLine(gridPen, new Point(rect.Right, verticalOffset), new Point(rect.Right, rect.Bottom));
						//dc.Pop();

						if (SuperDom.IsConnected 
							&& !SuperDom.IsReloading
							&& State == NinjaTrader.NinjaScript.State.Active)
						{
							// Draw proportional volume bar
							long	buyVolume		= 0;
							long	sellVolume		= 0;
							long	totalRowVolume	= 0;
							long	totalVolume		= 0;

							
							if (LastVolumes.TryGetValue(row.Price, out totalRowVolume))
								totalVolume = totalLastVolume;
							else
							{
								verticalOffset += SuperDom.ActualRowHeight;
								continue;
							}
							

							double totalWidth = renderWidth * ((double)totalRowVolume / maxVolume); 
							if (totalWidth - gridPen.Thickness >= 0)
							{								
								dc.DrawRectangle(BarColor, null, new Rect(0, verticalOffset + halfPenWidth, totalWidth == renderWidth ? totalWidth - gridPen.Thickness * 1.5 : totalWidth - halfPenWidth, rect.Height - gridPen.Thickness));
							}
							// Print volume value - remember to set MaxTextWidth so text doesn't spill into another column
							if (totalRowVolume > 0)
							{
								string volumeString = string.Empty;
							
								volumeString = totalRowVolume.ToString(Core.Globals.GeneralOptions.CurrentCulture);
								
								if (renderWidth - 6 > 0)
								{
									if (DisplayText || rect.Contains(Mouse.GetPosition(UiWrapper)))
									{
										FormattedText volumeText = new FormattedText(volumeString, Core.Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, SuperDom.Font.Size, ForeColor) { MaxLineCount = 1, MaxTextWidth = renderWidth - 6, Trimming = TextTrimming.CharacterEllipsis };

										// Getting the text height is expensive, so only update it if something's changed
										if (heightUpdateNeeded)
										{
											textHeight = volumeText.Height;
											heightUpdateNeeded = false;
										}

										textPosition.Y = verticalOffset + (SuperDom.ActualRowHeight - textHeight) / 2;
										dc.DrawText(volumeText, textPosition);
									}
								}
							}
							verticalOffset += SuperDom.ActualRowHeight;
						}
						else
							verticalOffset += SuperDom.ActualRowHeight;

						dc.Pop();
					}
				}
		}

		public override void OnRestoreValues()
		{
			// Forecolor and standard bar color
			bool restored = false;

			SolidColorBrush defaultForeColor = Application.Current.FindResource("immutableBrushVolumeColumnForeground") as SolidColorBrush;
			if (	(ForeColor			as SolidColorBrush).Color == (ImmutableForeColor as SolidColorBrush).Color
				&&	(ImmutableForeColor as SolidColorBrush).Color != defaultForeColor.Color)
			{
				ForeColor			= defaultForeColor;
				ImmutableForeColor	= defaultForeColor;
				restored			= true;
			}

			SolidColorBrush defaultBarColor = Application.Current.FindResource("immutableBrushVolumeColumnBackground") as SolidColorBrush;
			if ((BarColor as SolidColorBrush).Color == (ImmutableBarColor as SolidColorBrush).Color
				&& (ImmutableBarColor as SolidColorBrush).Color != defaultBarColor.Color)
			{
				BarColor			= defaultBarColor;
				ImmutableBarColor	= defaultBarColor;
				restored			= true;
			}

			if (restored) OnPropertyChanged();
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name					= "CurrentTrades";
				Buys					= new ConcurrentDictionary<double, long>();
				BackColor				= Brushes.Transparent;
				BarColor				= Application.Current.TryFindResource("brushVolumeColumnBackground") as Brush;
				BuyColor				= Brushes.Green;
				DefaultWidth			= 160;
				PreviousWidth			= -1;
				DisplayText				= true;
				ForeColor				= Application.Current.TryFindResource("brushVolumeColumnForeground") as Brush;
				ImmutableBarColor		= Application.Current.TryFindResource("immutableBrushVolumeColumnBackground") as Brush;
				ImmutableForeColor		= Application.Current.TryFindResource("immutableBrushVolumeColumnForeground") as Brush;
				IsDataSeriesRequired	= true;
				LastVolumes				= new ConcurrentDictionary<double,long>();
				SellColor				= Brushes.Red;
				Sells					= new ConcurrentDictionary<double,long>();
				Bidask					= VolumeSide.bid;
			}
			else if (State == State.Configure)
			{
				if (UiWrapper != null && PresentationSource.FromVisual(UiWrapper) != null)
				{ 
					Matrix m			= PresentationSource.FromVisual(UiWrapper).CompositionTarget.TransformToDevice;
					double dpiFactor	= 1 / m.M11;
					gridPen				= new Pen(Application.Current.TryFindResource("BorderThinBrush") as Brush,  1 * dpiFactor);
					halfPenWidth		= gridPen.Thickness * 0.5;
				}

				if (SuperDom.Instrument != null && SuperDom.IsConnected)
				{
					BarsPeriod bp		= new BarsPeriod
					{
						MarketDataType = MarketDataType.Last, 
						BarsPeriodType = BarsPeriodType.Tick, 
						Value = 1
					};

					SuperDom.Dispatcher.InvokeAsync(() => SuperDom.SetLoadingString());
					clearLoadingSent = false;

					if (BarsRequest != null)
					{
						BarsRequest.Update -= OnBarsUpdate;
						BarsRequest = null;
					}

					BarsRequest = new BarsRequest(SuperDom.Instrument,
						Cbi.Connection.PlaybackConnection != null ? Cbi.Connection.PlaybackConnection.Now : Core.Globals.Now,
						Cbi.Connection.PlaybackConnection != null ? Cbi.Connection.PlaybackConnection.Now : Core.Globals.Now);

					BarsRequest.BarsPeriod		= bp;
					BarsRequest.TradingHours	= (TradingHoursData == TradingHours.UseInstrumentSettings || TradingHours.Get(TradingHoursData) == null) ? SuperDom.Instrument.MasterInstrument.TradingHours : TradingHours.Get(TradingHoursData);
					BarsRequest.Update			+= OnBarsUpdate;

					BarsRequest.Request((request, errorCode, errorMessage) =>
						{
							// Make sure this isn't a bars callback from another column instance
							if (request != BarsRequest)
								return;

							lastMaxIndex	= 0;
							maxVolume		= 0;
							totalBuyVolume	= 0;
							totalLastVolume = 0;
							totalSellVolume = 0;
							Sells.Clear();
							Buys.Clear();
							LastVolumes.Clear();

							if (State >= NinjaTrader.NinjaScript.State.Terminated)
								return;

							if (errorCode == Cbi.ErrorCode.UserAbort)
							{
								if (State <= NinjaTrader.NinjaScript.State.Terminated)
									if (SuperDom != null && !clearLoadingSent)
									{
										SuperDom.Dispatcher.InvokeAsync(() => SuperDom.ClearLoadingString());
										clearLoadingSent = true;
									}
										
								request.Update -= OnBarsUpdate;
								request.Dispose();
								request = null;
								return;
							}
							
							if (errorCode != Cbi.ErrorCode.NoError)
							{
								request.Update -= OnBarsUpdate;
								request.Dispose();
								request = null;
								if (SuperDom != null && !clearLoadingSent)
								{
									SuperDom.Dispatcher.InvokeAsync(() => SuperDom.ClearLoadingString());
									clearLoadingSent = true;
								}
							}
							else if (errorCode == Cbi.ErrorCode.NoError)
							{
								SessionIterator	superDomSessionIt	= new SessionIterator(request.Bars);
								bool			isInclude60			= request.Bars.BarsType.IncludesEndTimeStamp(false);
								if (superDomSessionIt.IsInSession(Core.Globals.Now, isInclude60, request.Bars.BarsType.IsIntraday))
								{
									for (int i = 0; i < request.Bars.Count; i++)
									{
										DateTime time = request.Bars.BarsSeries.GetTime(i);
										if ((isInclude60 && time <= superDomSessionIt.ActualSessionBegin) || (!isInclude60 && time < superDomSessionIt.ActualSessionBegin))
											continue;

										double	ask		= request.Bars.BarsSeries.GetAsk(i);
										double	bid		= request.Bars.BarsSeries.GetBid(i);
										double	close	= request.Bars.BarsSeries.GetClose(i);
										long	volume	= request.Bars.BarsSeries.GetVolume(i);

										if (ask != double.MinValue && close >= ask)
										{
											Buys.AddOrUpdate(close, volume, (price, oldVolume) => oldVolume + volume);
											totalBuyVolume += volume;
										}
										else if (bid != double.MinValue && close <= bid)
										{
											Sells.AddOrUpdate(close, volume, (price, oldVolume) => oldVolume + volume);
											totalSellVolume += volume;
										}

										long newVolume;
										LastVolumes.AddOrUpdate(close, newVolume = volume, (price, oldVolume) => newVolume = 0);
										totalLastVolume += volume;
										
										if (newVolume > maxVolume)
											maxVolume = newVolume;
									}

									lastMaxIndex = request.Bars.Count - 1;

									// Repaint the column on the SuperDOM
									OnPropertyChanged();
								}

								if (SuperDom != null && !clearLoadingSent)
								{
									SuperDom.Dispatcher.InvokeAsync(() => SuperDom.ClearLoadingString());
									clearLoadingSent = true;
								}
							}
						});
				}
			}
			else if (State == State.Active)
			{
				if (!DisplayText)
				{
					WeakEventManager<System.Windows.Controls.Panel, MouseEventArgs>.AddHandler(UiWrapper, "MouseMove", OnMouseMove);
					WeakEventManager<System.Windows.Controls.Panel, MouseEventArgs>.AddHandler(UiWrapper, "MouseEnter", OnMouseEnter);
					WeakEventManager<System.Windows.Controls.Panel, MouseEventArgs>.AddHandler(UiWrapper, "MouseLeave", OnMouseLeave);
					mouseEventsSubscribed = true;
				}
			}
			else if (State == State.Terminated)
			{
				if (BarsRequest != null)
				{
					BarsRequest.Update -= OnBarsUpdate;
					BarsRequest.Dispose();
				}

				BarsRequest = null;

				if (SuperDom != null && !clearLoadingSent)
				{
					SuperDom.Dispatcher.InvokeAsync(() => SuperDom.ClearLoadingString());
					clearLoadingSent = true;
				}

				if (!DisplayText && mouseEventsSubscribed)
				{
					WeakEventManager<System.Windows.Controls.Panel, MouseEventArgs>.RemoveHandler(UiWrapper, "MouseMove", OnMouseMove);
					WeakEventManager<System.Windows.Controls.Panel, MouseEventArgs>.RemoveHandler(UiWrapper, "MouseEnter", OnMouseEnter);
					WeakEventManager<System.Windows.Controls.Panel, MouseEventArgs>.RemoveHandler(UiWrapper, "MouseLeave", OnMouseLeave);
					mouseEventsSubscribed = false;
				}

				lastMaxIndex	= 0;
				maxVolume		= 0;
				totalBuyVolume	= 0;
				totalLastVolume = 0;
				totalSellVolume = 0;
				Sells.Clear();
				Buys.Clear();
				LastVolumes.Clear();
			}
		}

		#region Bar Collections
		[XmlIgnore]
		[Browsable(false)]
		public ConcurrentDictionary<double, long> Buys { get; set; }

		[XmlIgnore]
		[Browsable(false)]
		public ConcurrentDictionary<double, long> LastVolumes { get; set; }

		[XmlIgnore]
		[Browsable(false)]
		public ConcurrentDictionary<double, long> Sells { get; set; }
		#endregion

		#region Properties
		
		[NinjaScriptProperty]
		[Display(Name="Bidask", Order=1, GroupName="Parameters")]
		public VolumeSide Bidask
		{ get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseBackground", GroupName = "PropertyCategoryVisual", Order = 130)]
		public Brush BackColor { get; set; }

		[Browsable(false)]
		public string BackColorSerialize
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(BackColor); }
			set { BackColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Resource), Name = "NinjaScriptBarColor", GroupName = "PropertyCategoryVisual", Order = 110)]
		public Brush BarColor { get; set; }

		[Browsable(false)]
		public string BarColorSerialize
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(BarColor); }
			set { BarColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Resource), Name = "NinjaScriptBuyColor", GroupName = "PropertyCategoryVisual", Order = 120)]
		public Brush BuyColor { get; set; }

		[Browsable(false)]
		public string BuyColorSerialize
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(BuyColor); }
			set { BuyColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDisplayText", GroupName = "PropertyCategoryVisual", Order = 175)]
		public bool DisplayText { get; set; }


		[XmlIgnore]
		[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseForeground", GroupName = "PropertyCategoryVisual", Order = 140)]
		public Brush ForeColor { get; set; }

		[Browsable(false)]
		public string ForeColorSerialize
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(ForeColor); }
			set { ForeColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Browsable(false)]
		public Brush ImmutableBarColor { get; set; }

		[Browsable(false)]
		public string ImmutableBarColorSerialize
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(ImmutableBarColor, "CustomVolume.ImmutableBarColor"); }
			set { ImmutableBarColor = NinjaTrader.Gui.Serialize.StringToBrush(value, "CustomVolume.ImmutableBarColor"); }
		}

		[XmlIgnore]
		[Browsable(false)]
		public Brush ImmutableForeColor { get; set; }

		[Browsable(false)]
		public string ImmutableForeColorSerialize
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(ImmutableForeColor, "CustomVolume.ImmutableForeColor"); }
			set { ImmutableForeColor = NinjaTrader.Gui.Serialize.StringToBrush(value, "CustomVolume.ImmutableForeColor"); }
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Resource), Name = "NinjaScriptSellColor", GroupName = "PropertyCategoryVisual", Order = 170)]
		public Brush SellColor { get; set; }

		[Browsable(false)]
		public string SellColorSerialize
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(SellColor); }
			set { SellColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[Display(ResourceType = typeof(Resource), Name = "IndicatorSuperDomBaseTradingHoursTemplate", GroupName = "NinjaScriptTimeFrame", Order = 60)]
		[RefreshProperties(RefreshProperties.All)]
		[TypeConverter(typeof(NinjaTrader.NinjaScript.TradingHoursDataConverter))]
		public string TradingHoursData
		{
			get { return tradingHoursData; }
			set { tradingHoursData = value; }
		}
		
		#endregion
	}
}
