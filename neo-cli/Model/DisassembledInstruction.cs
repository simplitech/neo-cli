using Neo.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Model
{
	class DisassembledInstruction
	{
		public OpCode OpCode { get; set; }
		public List<DisassembledInstruction> Operands { get; set; }

		public DisassembledInstruction()
		{
			Operands = new List<DisassembledInstruction>();
		}

	}
}
