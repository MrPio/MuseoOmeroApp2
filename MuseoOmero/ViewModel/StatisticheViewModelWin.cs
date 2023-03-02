﻿using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.Easing;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Linq;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Maui;

namespace MuseoOmero.ViewModelWin;

public partial class StatisticheViewModelWin : ObservableObject
{
	private HomeViewModelWin _homeViewModelWin;


	public ISeries[] Series2 { get; set; } =
	{
		new PieSeries<double> { Values = new List<double> { 3 }, Pushout = 4 },
		new PieSeries<double> { Values = new List<double> { 3 }, Pushout = 4 },
		new PieSeries<double> { Values = new List<double> { 3 }, Pushout = 4 },
		new PieSeries<double> { Values = new List<double> { 2 }, Pushout = 4 },
		new PieSeries<double> { Values = new List<double> { 5 }, Pushout = 30 }
	};

	public ISeries[] Series3 { get; set; } = new ISeries[]
  {
		new PieSeries<double> { Values = new List<double> { 2 }, InnerRadius = 50,MaxOuterRadius = 0.8 },
		new PieSeries<double> { Values = new List<double> { 4 }, InnerRadius = 50,MaxOuterRadius = 0.85 },
		new PieSeries<double> { Values = new List<double> { 1 }, InnerRadius = 50,MaxOuterRadius = 0.9 },
		new PieSeries<double> { Values = new List<double> { 4 }, InnerRadius = 50,MaxOuterRadius = 0.95 },
		new PieSeries<double> { Values = new List<double> { 3 }, InnerRadius = 50,MaxOuterRadius = 1 }
  };

	// BIGLIETTI series
	[ObservableProperty]
	ColumnSeries<float>[] bigliettiSeries;
	[ObservableProperty]
	Axis[] bigliettiXAxes =
	{
		new()
		{
			Labels = new[] { "L", "M", "MM","G","V","S","D" },
			LabelsRotation = 30,
			SeparatorsPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
			SeparatorsAtCenter = false,
			TicksPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
			TicksAtCenter = true
		}
	};

	// QUESTIONARI series
	[ObservableProperty]
	IEnumerable<ISeries> questionariValutazioniSeries;
	[ObservableProperty]
	ISeries[] questionariTipologieVisiteSeries, questionariAccompagnatoriVisitaSeries, questionariMotivazioneVisitaSeries, questionariTitoloStudiSeries, questionariRitornoSeries;

	public StatisticheViewModelWin(HomeViewModelWin homeViewModelWin)
	{
		_homeViewModelWin = homeViewModelWin;
	}

	public async void Initialize()
	{
		var utenti = _homeViewModelWin.Utenti;
		var biglietti = utenti.SelectMany(u => u.Biglietti);
		var questionari = utenti.SelectMany(u => u.Questionari);

		//BIGLIETTI - vendite/convalide
		{
			var vendite = new float[7];
			var convalide = new float[7];
			foreach (var b in biglietti)
			{
				++vendite[(int)b.DataAcquisto.DayOfWeek];
				if (b.DataConvalida is { })
					++convalide[(int)b.DataConvalida?.DayOfWeek];
			}
			BigliettiSeries = new ColumnSeries<float>[] { };
			BigliettiSeries = new ColumnSeries<float>[]
			{
			new()
			{
				Name = "Venduti",
				Values = vendite
			},
			new()
			{
				Name = "Convalidati",
				Values = convalide
			}
			};
			foreach (var s in BigliettiSeries)
			{
				s.PointMeasured += OnPointMeasuredBar; //Ritardo nella comparsa
				s.ChartPointPointerHover += OnPointerHoverBar;
				//s.ChartPointPointerDown += OnPointerDown;
				s.ChartPointPointerHoverLost += OnPointerHoverLostBar;
			}
		}

		//QUESTIONARI - valutazioni
		{
			var sommaVisita = 0;
			var sommaEsperienza = 0;
			var sommaStruttura = 0;
			foreach (var q in questionari)
			{
				sommaVisita += q.ValutazioneVisita;
				sommaEsperienza += q.ValutazioneEsperienza;
				sommaStruttura += q.ValutazioneStruttura;
			}
			QuestionariValutazioniSeries = new GaugeBuilder()
			.WithLabelsSize(20)
			.WithLabelsPosition(PolarLabelsPosition.Start)
			.WithLabelFormatter(point => string.Format("{0:0.##}", point.PrimaryValue))
			.WithInnerRadius(20)
			.WithOffsetRadius(8)
			.WithCornerRadius(8)
			.WithBackground(null)
			.WithBackgroundInnerRadius(8)
			.AddValue(Math.Round((double)sommaVisita / questionari.Count(), 2), "Visita")
			.AddValue(Math.Round((double)sommaEsperienza / questionari.Count(), 2), "Esperienza")
			.AddValue(Math.Round((double)sommaStruttura / questionari.Count(), 2), "Struttura")
			.BuildSeries();
		}

		//QUESTIONARI - tipologia_visita
		{
			var values = TipologiaVisita.Values;
			var occorrenze = new int[values.Length];
			foreach (var q in questionari)
			{
				if (values.Contains(q.TipologiaVisita))
					++occorrenze[values.IndexOf(q.TipologiaVisita)];
			}
			QuestionariTipologieVisiteSeries = new PieSeries<double>[]{
				new() { Values= new List<double>() {occorrenze[0] },Name=values[0]},
				new() { Values= new List<double>() {occorrenze[0] },Name=values[1]},
				new() { Values= new List<double>() {occorrenze[0] },Name=values[2]},
			};

			foreach (PieSeries<double> series in QuestionariTipologieVisiteSeries)
			{
				series.Pushout = 4;
				series.DataLabelsFormatter = p => p.Context.Series.Name;
			}

		}

		//QUESTIONARI - accompagnatori_visita
		{
			var values = AccompagnatoriVisita.Values;
			var occorrenze = new int[values.Length];
			foreach (var q in questionari)
			{
				if (values.Contains(q.AccompagnatoriVisita))
					++occorrenze[values.IndexOf(q.AccompagnatoriVisita)];
			}
			QuestionariAccompagnatoriVisitaSeries = new PieSeries<double>[values.Length];
			for (var i = 0; i < values.Length; ++i)
			{
				PieSeries<double> series = new() { Values = new List<double>() { occorrenze[i] }, Name = values[i] };
				series.PointMeasured += OnPointMeasuredPie;
				series.Pushout = 4;
				series.InnerRadius = 60;
				series.DataLabelsFormatter = p => p.Context.Series.Name;
				QuestionariAccompagnatoriVisitaSeries[i] = series;
			}
		}

		//QUESTIONARI - motivazione_visita
		{
			var values = MotivazioneVisita.Values;
			var occorrenze = new int[values.Length];
			foreach (var q in questionari)
			{
				if (values.Contains(q.MotivazioneVisita))
					++occorrenze[values.IndexOf(q.MotivazioneVisita)];
			}
			QuestionariMotivazioneVisitaSeries = new PieSeries<double>[values.Length];
			for (var i = 0; i < values.Length; ++i)
			{
				PieSeries<double> series = new() { Values = new List<double>() { occorrenze[i] }, Name = values[i] };
				series.PointMeasured += OnPointMeasuredPie;
				series.Pushout = 4;
				series.MaxOuterRadius = ((double)i / values.Length) * 0.2 + 0.8;
				series.DataLabelsFormatter = p => p.Context.Series.Name;
				QuestionariMotivazioneVisitaSeries[i] = series;
			}
		}

		//QUESTIONARI - titolo_studi
		{
			var values = TitoloStudi.Values;
			var occorrenze = new int[values.Length];
			foreach (var q in questionari)
			{
				if (values.Contains(q.TitoloStudi))
					++occorrenze[values.IndexOf(q.TitoloStudi)];
			}
			QuestionariTitoloStudiSeries = new PieSeries<double>[values.Length];
			for (var i = 0; i < values.Length; ++i)
			{
				PieSeries<double> series = new() { Values = new List<double>() { occorrenze[i] }, Name = values[i] };
				series.PointMeasured += OnPointMeasuredPie;
				series.Pushout = 4;
				series.InnerRadius = 60;
				series.DataLabelsFormatter = p => p.Context.Series.Name;
				series.MaxOuterRadius = ((double)i / values.Length) * 0.2 + 0.8;

				QuestionariTitoloStudiSeries[i] = series;
			}
		}

		//QUESTIONARI - ritorno
		{
			var values = Ritorno.Values;
			var occorrenze = new int[values.Length];
			foreach (var q in questionari)
			{
				if (values.Contains(q.Ritorno))
					++occorrenze[values.IndexOf(q.Ritorno)];
			}
			QuestionariRitornoSeries = new PieSeries<double>[values.Length];
			for (var i = 0; i < values.Length; ++i)
			{
				PieSeries<double> series = new() { Values = new List<double>() { occorrenze[i] }, Name = values[i] };
				series.PointMeasured += OnPointMeasuredPie;
				series.Pushout = 4;
				series.InnerRadius = 30;
				series.DataLabelsFormatter = p => p.Context.Series.Name;
				QuestionariRitornoSeries[i] = series;
			}
		}
	}
	private void OnPointMeasuredBar(ChartPoint<float, RoundedRectangleGeometry, LabelGeometry> point)
	{
		var visual = point.Visual;
		if (visual is null) return;

		var delayedFunction = new DelayedFunction(EasingFunctions.BuildCustomElasticOut(1.85f, 0.80f), point, 25f);

		_ = visual
			.TransitionateProperties(
				nameof(visual.Y),
				nameof(visual.Height))
			.WithAnimation(animation =>
				animation
					.WithDuration(delayedFunction.Speed)
					.WithEasingFunction(delayedFunction.Function));
	}


	private void OnPointerDownBar(IChartView chart, ChartPoint<float, RoundedRectangleGeometry, LabelGeometry>? point)
	{
		if (point?.Visual is null) return;
		point.Visual.Fill = new SolidColorPaint(SKColors.Orange);
		chart.Invalidate(); // <- ensures the canvas is redrawn after we set the fill
	}

	private void OnPointerHoverBar(IChartView chart, ChartPoint<float, RoundedRectangleGeometry, LabelGeometry>? point)
	{
		if (point?.Visual is null) return;
		point.Visual.Fill = new SolidColorPaint(SKColors.Yellow);
		chart.Invalidate();
	}
	private void OnPointerHoverLostBar(IChartView chart, ChartPoint<float, RoundedRectangleGeometry, LabelGeometry>? point)
	{
		if (point?.Visual is null) return;
		point.Visual.Fill = null;
		chart.Invalidate();
	}

	//PIE
	private void OnPointMeasuredPie(ChartPoint<double, DoughnutGeometry, LabelGeometry> point)
	{
		var visual = point.Visual;
		if (visual is null) return;

		var delayedFunction = new DelayedFunction(EasingFunctions.BuildCustomElasticOut(4.85f, 2.80f), point, 1225f);

		_ = visual
			.TransitionateProperties(
				nameof(visual.Y),
				nameof(visual.Height))
			.WithAnimation(animation =>
				animation
					.WithDuration(delayedFunction.Speed)
					.WithEasingFunction(delayedFunction.Function));
	}

	private void OnPointerHoverPie(IChartView chart, ChartPoint<double, DoughnutGeometry, LabelGeometry>? point)
	{
		if (point?.Visual is null) return;
		point.Visual.PushOut = 30;
		chart.Invalidate();
	}

	private void OnPointerDownPie(IChartView chart, ChartPoint<double, DoughnutGeometry, LabelGeometry>? point)
	{
		if (point?.Visual is null) return;
		((PieChart)chart).InitialRotation += 40;
		//((PieChart)chart).ShowTooltip();
		//chart.Invalidate(); // <- ensures the canvas is redrawn after we set the fill
	}

	private void OnPointerHoverLostPie(IChartView chart, ChartPoint<double, DoughnutGeometry, LabelGeometry>? point)
	{
		if (point?.Visual is null) return;
		point.Visual.PushOut = 4;
		chart.Invalidate();
	}

}
