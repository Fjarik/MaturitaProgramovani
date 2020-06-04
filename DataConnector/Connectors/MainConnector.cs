using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DataConnector.Models;
using Flurl;
using Flurl.Http;

namespace DataConnector.Connectors
{
	public class MainConnector
	{
		private const string SmallMazeUrl = "http://maturita-2020-test.delta-studenti.cz/maze-api";
		private const string LargeMazeUrl = "http://maturita.delta-studenti.cz/prakticka/2020-maze/maze-api";
		private const int Timeout = 2; // sekundy
		private const string TokenKey = "";

		public async Task<UniversalResult> SendData(Actions action, Mazes size, int tryNo = 1)
		{
			if (tryNo > 5) {
				// Ani na 5. pokus požadavek neprošel
				return new UniversalResult(new ApplicationException("Nezdařilo se získat data, příliš mnoho pokusů."));
			}

			var url = SmallMazeUrl;
			if (size == Mazes.Large) {
				url = LargeMazeUrl;
			}

			var actionString = action.ToString().ToLower();

			// Nastavení dat k odeslání
			var data = new {
				// hodnoty
				token = TokenKey,
				command = actionString
			};

			// Časový limit
			var request = url.WithTimeout(Timeout);
			//.AllowHttpStatus(HttpStatusCode.NotFound)

			MainModel model;
			try {
				// Získání dat
				model = await request.PostUrlEncodedAsync(data).ReceiveJson<MainModel>();
			} catch (FlurlHttpTimeoutException) {
				// Vypršel časový limit požadavku
				return new UniversalResult(new TimeoutException("Špatné připojení k internetu"));
			} catch (FlurlHttpException) {
				// Počkat 20 ms
				await Task.Delay(20);
				// Zkusit to znovu
				return await SendData(action, size, tryNo + 1);
			}

			return new UniversalResult(model);
		}

		public Task<UniversalResult> Reset(Mazes size)
		{
			return this.SendData(Actions.Reset, size);
		}
	}
}