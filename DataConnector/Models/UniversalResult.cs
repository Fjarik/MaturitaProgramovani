using System;
using System.Collections.Generic;
using System.Text;

namespace DataConnector.Models
{
	public class UniversalResult
	{
		public MainModel Model { get; private set; } = null;
		public Exception Exception { get; private set; } = null;

		public bool IsSuccess => Model != null &&
								 Exception == null;

		// Chybová hláška
		public string ErrorMessage => this.Exception?.Message;

		public UniversalResult(MainModel model)
		{
			this.Model = model;
		}

		public UniversalResult(Exception exception)
		{
			this.Exception = exception;
		}
	}
}