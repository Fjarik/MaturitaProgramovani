using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataConnector.Connectors;
using DataConnector.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Bludiste
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		private readonly MainConnector _connector;
		private readonly List<MainModel> _allRequests;
		private readonly MainModel[,] _knowLocations;
		private readonly Button[] _buttons;

		private readonly int _size = 21;
		private readonly int _center;

		private Mazes _currentMaze;

		private int _prevX;
		private int _prevY;

		private int _currentX;
		private int _currentY;

		public MainWindow()
		{
			InitializeComponent();

			_center = (_size - 1) / 2;

			// "Tabulka" pro vykreslování bludiště
			for (int j = 0; j < _size; j++) {
				GridMap.ColumnDefinitions.Add(new ColumnDefinition());
				GridMap.RowDefinitions.Add(new RowDefinition());
			}
			this._currentX = _center;
			this._currentY = _center;

			this._knowLocations = new MainModel[_size, _size];
			this._connector = new MainConnector();
			this._currentMaze = Mazes.Small;
			this._allRequests = new List<MainModel>();

			this._buttons = new[] {
				BtnUp,
				BtnDown,
				BtnRight,
				BtnLeft,
				//BtnChange,
				BtnReset,
				//BtnGrab,
				//BtnLeave
			};

			// Zobrazit načítání
			ChangeLoading(true);
		}

		private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await Reset();
		}

		private void ChangeLoading(bool showLoading)
		{
			if (showLoading) {
				GridLoading.Visibility = Visibility.Visible;
				GridMain.Visibility = Visibility.Hidden;
			} else {
				GridLoading.Visibility = Visibility.Hidden;
				GridMain.Visibility = Visibility.Visible;
			}
		}

		private void ClearLog()
		{
			_allRequests.Clear();
			TxtLog.Text = "";
		}

		private void AddLog(MainModel model, Actions a)
		{
			_allRequests.Add(model);

			var id = _allRequests.Count;
			var action = a.ToString().ToLower();
			var command = $"Příkaz {action} {Environment.NewLine}";

			var response = "Danou operaci nelze provést";
			if (model.Success) {
				response = $"You see {model.YouSee}";
				if (string.IsNullOrWhiteSpace(model.YouSee)) {
					response = "Nic zde není";
				}
			}

			string toAdd = $"{id}: {command}" + response + Environment.NewLine;

			this.AddLog(toAdd);
		}

		private void AddLog(string text)
		{
			var oldText = TxtLog.Text;

			TxtLog.Text = text + Environment.NewLine;

			TxtLog.Text += oldText;
		}

		private Task ChangeMaze()
		{
			var newMaze = Mazes.Large;
			if (_currentMaze == Mazes.Large) {
				newMaze = Mazes.Small;
			}

			return ChangeMaze(newMaze);
		}

		private async Task ChangeMaze(Mazes newMaze)
		{
			_currentMaze = newMaze;
			await this.Reset();
		}

		// Resetovat bludiště
		private async Task Reset()
		{
			// Zobrazit načítání
			ChangeLoading(true);

			var res = await _connector.Reset(_currentMaze);
			if (!res.IsSuccess) {
				await this.ShowMessageAsync("Nezdařilo se resetovat bludiště", res.ErrorMessage);
				return;
			}
			var model = res.Model;

			GridMap.Children.Clear();
			_currentX = _center;
			_currentY = _center;

			ClearLog();
			this.AddLog($"Aktuální bludiště: {_currentMaze.ToString()}");

			CheckModel(model, Actions.Reset);

			// Skrýt načítání
			ChangeLoading(false);

			// Obnovit tlačítka
			ChangeButtons(true);
		}

#region Tlačítka

		private async void BtnUp_OnClick(object sender, RoutedEventArgs e)
		{
			await SendAction(Actions.North);
		}

		private async void BtnRight_OnClick(object sender, RoutedEventArgs e)
		{
			await SendAction(Actions.East);
		}

		private async void BtnLeft_OnClick(object sender, RoutedEventArgs e)
		{
			await SendAction(Actions.West);
		}

		private async void BtnDown_OnClick(object sender, RoutedEventArgs e)
		{
			await SendAction(Actions.South);
		}

		private async void BtnChange_OnClick(object sender, RoutedEventArgs e)
		{
			await this.ChangeMaze();
		}

		private async void BtnReset_OnClick(object sender, RoutedEventArgs e)
		{
			await this.Reset();
		}

		private async void BtnGrab_OnClick(object sender, RoutedEventArgs e)
		{
			await SendAction(Actions.Grab);
		}

		private async void BtnLeave_OnClick(object sender, RoutedEventArgs e)
		{
			await SendAction(Actions.Exit);
		}

#endregion

		private async Task SendAction(Actions action)
		{
			// Vypnout tlačítka
			ChangeButtons(false);

			// Poslání požadavku
			var res = await _connector.SendData(action, _currentMaze);
			if (!res.IsSuccess) {
				await this.ShowMessageAsync("Vyskytla se chyba", res.ErrorMessage);
				return;
			}

			var model = res.Model;

			// 4. úkol
			//var others = await GetOthers();

			// Kontrola a vykreslení dlaždice
			CheckModel(model, action);

			if (action != Actions.Exit || !model.Success) {
				// Zapnout tlačítka
				ChangeButtons(true);
			}
		}

		private async Task<List<MainModel>> GetOthers()
		{
			// Získat okolní dlaždite (4. úkol)
			MainModel down = _knowLocations[_currentX, _currentY - 1];
			MainModel up = _knowLocations[_currentX, _currentY + 1];
			MainModel west = _knowLocations[_currentX - 1, _currentY];
			MainModel east = _knowLocations[_currentX + 1, _currentY];

			if (down == null) {
				down = await GetOther(Actions.South, Actions.North);
				_knowLocations[_currentX, _currentY - 1] = down;
			}
			if (up == null) {
				up = await GetOther(Actions.North, Actions.South);
				_knowLocations[_currentX, _currentY + 1] = up;
			}
			if (west == null) {
				west = await GetOther(Actions.West, Actions.East);
				_knowLocations[_currentX - 1, _currentY] = west;
			}
			if (east == null) {
				east = await GetOther(Actions.East, Actions.West);
				_knowLocations[_currentX + 1, _currentY] = east;
			}

			return new List<MainModel>() {
				down, up, west, east
			};
		}

		private async Task<MainModel> GetOther(Actions direction, Actions opositeAction)
		{
			// Získání bodu v daném směru 
			var a = await _connector.SendData(direction, _currentMaze);
			if (a.IsSuccess) {
				await _connector.SendData(opositeAction, _currentMaze);
				return a.Model;
			}
			return null;
		}

		private void CheckModel(MainModel model, Actions action)
		{
			// Vypnutí tlačítek
			BtnGrab.IsEnabled = false;
			BtnLeave.IsEnabled = false;

			// Zapsání do logu
			this.AddLog(model, action);

			// Přidání do známých lokací (4. úkol)
			_knowLocations[_currentX, _currentY] = model;

			_prevX = _currentX;
			_prevY = _currentY;

			if (action == Actions.North) {
				_currentY -= 1;
			}
			if (action == Actions.West) {
				_currentX -= 1;
			}
			if (action == Actions.East) {
				_currentX += 1;
			}
			if (action == Actions.South) {
				_currentY += 1;
			}

			if (!model.Success) {
				// Přidat/namalovat zeď (černá)
				AddToMap(Colors.Black);
				_currentX = _prevX;
				_currentY = _prevY;
				return;
			}

			if (model.YouSee == "countryside") {
				// Konec
				ChangeButtons(false);
				BtnReset.IsEnabled = true;
				return;
			}

			var color = model.LightColor;
			if (ColorConverter.ConvertFromString(color) is Color c) {
				// Nastavení barvy
				CanvasColor.Background = new SolidColorBrush(c);
				// Vykreslení barvy
				AddToMap(c, model.YouSee == "exit");
			}

			if (model.YouSee == "key") {
				BtnGrab.IsEnabled = true;
				// Vykreslení klíče
				AddToMap(Colors.Gold);
			}
			if (model.YouSee == "exit" && model.HaveKey) {
				BtnLeave.IsEnabled = true;
			}
		}

		private void AddToMap(Color c, bool exit = false)
		{
			// Nalezení a vymazání X (aktuální pozice)
			var pozice = GridMap.FindChildren<TextBlock>();
			foreach (var p in pozice) {
				GridMap.Children.Remove(p);
			}
			var rec = new Rectangle {
				Fill = new SolidColorBrush(c),
			};

			if (exit) {
				// Východ má barevné okraje
				rec.Stroke = new SolidColorBrush(Colors.LawnGreen);
				rec.StrokeThickness = 2;
			}

			var text = new TextBlock {
				Text = "X",
				FontSize = 10,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				TextAlignment = TextAlignment.Center
			};

			Grid.SetColumn(rec, _currentX);
			Grid.SetColumn(text, _currentX);
			Grid.SetRow(rec, _currentY);
			Grid.SetRow(text, _currentY);
			GridMap.Children.Add(rec);
			GridMap.Children.Add(text);
		}

		private void ChangeButtons(bool shouldBeEnabled)
		{
			// Vypnout nebo zapnout tlačítka
			foreach (var btn in this._buttons) {
				btn.IsEnabled = shouldBeEnabled;
			}
		}
	}
}