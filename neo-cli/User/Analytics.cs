using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.User
{
	class Analytics
	{
		private static List<string> _commandLineInputs = new List<string>();
		private static List<string> _connectedNodes = new List<string>();

		public static void AddInput(string[] input)
		{
			var stringInput = input.Aggregate((current, next) => current + " " + next);
			_commandLineInputs.Add(stringInput + ";"+ DateTime.UtcNow);
		}

		public static void AddConnectedNode(string ipv4)
		{
			if (!_connectedNodes.Contains(ipv4))
			{
				_connectedNodes.Add(ipv4);
			}
		}

		public static List<string> ListRegisteredInputs()
		{
			var list = new List<string>();
			list.AddRange(_commandLineInputs);
			return list;
		}
	
	}
}
