using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Neo.Cli.Extensions
{
	public class TransferScript
	{
		public string ContractHash { get; set; }
		public UInt160 From { get; set; }
		public UInt160 To { get; set; }
		public BigInteger Amount { get; set; }
		public byte Decimals { get; set; }


		public TransferScript()
		{
			ContractHash = "";
			From = UInt160.Zero;
			To = UInt160.Zero;
			Amount = 0;
			Decimals = 8;
		}
	}
}
