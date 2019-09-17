using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native.Tokens;
using Neo.User;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Neo.Cli.Extensions
{
	public static class CLIHelper
	{
		public static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);


		public static bool IsAddress(this string strParam)
		{
			return strParam.StartsWith("A") && strParam.Length == 34;
		}

		public static List<TransferScript> ExtractTransfersFromScript(List<object> instructions)
		{
			var output = new List<TransferScript>();
			var supportedMethods = InteropService.SupportedMethods();
			var contractCall = supportedMethods[InteropService.System_Contract_Call];

			for (int i = 0; i < instructions.Count; i++)
			{
				var instruction = instructions[i];
				int offset = 1;
				if (instruction.Equals(contractCall))
				{
					//Next instruction is the syscall
					var contractScriptHash = instructions[i + offset];
					offset++;
					var operation = (string)instructions[i + offset];
					offset++;
					var bytes = operation.HexToBytes();
					var utf8String = Encoding.UTF8.GetString(bytes);
					if (utf8String.Equals("transfer"))
					{
						//PACK
						offset++;
						//PUSH3
						offset++;
						var from = UInt160.Parse((string)instructions[i + offset]);
						offset++;
						var to = UInt160.Parse((string)instructions[i + offset]);
						offset++;
						var amount = BigInteger.Parse((string)instructions[i + offset]);
						var transferScript = new TransferScript();
						transferScript.ContractHash = contractScriptHash.ToString();
						transferScript.From = from;
						transferScript.To = to;
						transferScript.Amount = amount;
						output.Add(transferScript);
					}
					i = i + offset;
				}
			}

			return output;
		}

		public static List<object> DisassembleScript(byte[] script)
		{
			var supportedMethods = InteropService.SupportedMethods();
			var instructions = new List<object>();

			for (int i = 0; i < script.Length; i++)
			{
				OpCode currentOpCode = (OpCode)script[i];

				if (currentOpCode == OpCode.SYSCALL)
				{
					var interop = script.Skip(i + 1).Take(4).ToArray();
					var callNumber = BitConverter.ToUInt32(interop);
					var methodName = supportedMethods[callNumber];
					instructions.Add(methodName);
					i = i + 4;
				}
				else if (currentOpCode == OpCode.PUSH0)
				{
					instructions.Add("0");
					i = i + 1;
				}
				else if (currentOpCode <= OpCode.PUSHBYTES75)
				{
					var byteArraySize = (int)currentOpCode;
					var byteArray = script.Skip(i + 1).Take(byteArraySize).ToArray();
					var hexString = byteArray.ToHexString();
					if (byteArraySize == 20)
					{
						var scriptHash = new UInt160(byteArray);
						hexString = scriptHash.ToString();
					}
					instructions.Add(hexString);
					i = i + byteArraySize;
				}
				else if (currentOpCode >= OpCode.PUSHDATA1 && currentOpCode <= OpCode.PUSHDATA4)
				{
					int opcodeOffset = (int)OpCode.PUSHDATA1;
					int currentOpcode = (int)currentOpCode;
					int informationSize = (int)Math.Pow(2, currentOpcode - opcodeOffset);
					instructions.Add(currentOpCode);
					var dataSizeInBytes = script.Skip(i + 1).Take(informationSize).ToArray();
					long bytesUsed = 0;

					if (informationSize == 1)
					{
						bytesUsed = (byte)dataSizeInBytes[0];
					}
					else if (informationSize == 2)
					{
						bytesUsed = BitConverter.ToUInt16(dataSizeInBytes);
					}
					else if (informationSize == 4)
					{
						bytesUsed = BitConverter.ToUInt32(dataSizeInBytes);
					}

					instructions.Add(bytesUsed);
					var information = script.Skip(i + 1 + informationSize).Take((int)bytesUsed).ToArray();
					var hexBytes = information.ToHexString();
					instructions.Add(hexBytes);
					i = i + 1 + informationSize + (int)bytesUsed;
				}
				else
				{
					instructions.Add(currentOpCode.ToString());
				}

			}

			return instructions;
		}

		public static string DisassembledScriptToCLIString(byte[] script)
		{
			var supportedMethods = InteropService.SupportedMethods();
			var output = "";
			var commandList = DisassembleScript(script);
			foreach(var command in commandList)
			{
				output+= ($"\t{command}\n");
			}

			return output;
		}

		public static string ToCLIString(this Preferences preferences)
		{
			string output = $"\tAnalytics: {preferences.UseAnalytics}\n";
			output += $"\tSkip First Use: {preferences.SkipFirstUse}\n";
			output += $"\tFaucet Authorization File URL: {preferences.FaucetGitHubConfirmationUrl}\n";
			output += $"\tFaucet Recipient: {preferences.FaucetAuthorizedAddress}\n";
			output += $"\tPreferences Folder: {Preferences.FolderPath}\n";
			return output;
		}

		public static string ToCLIString(this Cosigner cosigner)
		{
			string output = $"\tAccount: {cosigner.Account}\n";

			output += "\tScope:\t";
			if (cosigner.Scopes == WitnessScope.Global)
			{
				output += $"Global\t";
			}
			if (cosigner.Scopes.HasFlag(WitnessScope.CalledByEntry))
			{
				output += $"CalledByEntry\t";
			}
			if (cosigner.Scopes.HasFlag(WitnessScope.CustomContracts))
			{
				output += $"CustomContract\t";
			}
			if (cosigner.Scopes.HasFlag(WitnessScope.CustomGroups))
			{
				output += $"CustomGroup\t";
			}

			output += "\n";

			if (cosigner.AllowedContracts != null && cosigner.AllowedContracts.Length > 0)
			{
				output += "Allowed contracts: \n";
				foreach (var allowedContract in cosigner.AllowedContracts)
				{
					output += $"\t{allowedContract.ToString()}\n";
				}
			}

			if (cosigner.AllowedGroups != null && cosigner.AllowedGroups.Length > 0)
			{
				output += "Allowed groups: \n";
				foreach (var allowedGroup in cosigner.AllowedGroups)
				{
					output += $"\t{allowedGroup.ToString()}\n";
				}
			}

			return output;
		}

		public static string ToCLIString(this Witness witness)
		{
			string output = "";
			output += $"Invocation: \n";
			output += DisassembledScriptToCLIString(witness.InvocationScript);
			output += $"Verification: \n";
			output += DisassembledScriptToCLIString(witness.VerificationScript);
			return output;
		}


		public static string ToCLIString(this NotifyEventArgs notification)
		{
			string output = "";
			var vmArray = (Neo.VM.Types.Array)notification.State;
			var notificationName = "";
			if (vmArray.Count > 0)
			{
				notificationName = vmArray[0].GetString();
			}

			var adapter = NotificationCLIAdapter.GetCliStringAdapter(notificationName);
			output += adapter(vmArray);

			return output;
		}

		public static string ToCLITimestampString(this ulong blockTimestamp)
		{
			var blockTime = UnixEpoch.AddMilliseconds(blockTimestamp);
			blockTime = TimeZoneInfo.ConvertTimeFromUtc(blockTime, TimeZoneInfo.Local);
			return blockTime.ToShortDateString() + " " + blockTime.ToLongTimeString(); ;
		}

		public static void PrettyPrintCLIString(string cliString)
		{
			var currentConsoleColor = Console.ForegroundColor;
			var newColor = ConsoleColor.DarkGreen;
			var lines = cliString.Split("\n");

			foreach (var line in lines)
			{
				Console.WriteLine();
				var splitedContent = line.Split(":");
				if (splitedContent.Length == 2)
				{
					Console.ForegroundColor = newColor;
					Console.Write(splitedContent[0] + ":");
					Console.ForegroundColor = currentConsoleColor;
					Console.Write(splitedContent[1]);
					Console.ForegroundColor = currentConsoleColor;
				}
				//Date
				else if (splitedContent.Length > 2)
				{
					Console.ForegroundColor = newColor;
					var titleString = splitedContent[0] + ":";
					Console.Write(titleString);
					Console.ForegroundColor = currentConsoleColor;
					Console.Write(splitedContent.Aggregate((current, next) => current + ":" + next).Replace(titleString, ""));
					Console.ForegroundColor = currentConsoleColor;
				}
				else
				{
					Console.ForegroundColor = currentConsoleColor;
					Console.Write(splitedContent.Aggregate((current, next) => current + ":" + next));
					Console.ForegroundColor = currentConsoleColor;
				}
			}
			Console.WriteLine();

			Console.ForegroundColor = currentConsoleColor;
		}


		public static string ToCLIString(this Block block, bool disassemble = true)
		{
			string output = "";
			output += $"\nHash: {block.Hash}\n";
			output += $"Index: {block.Index}\n";
			output += $"Size: {block.Size}\n";
			output += $"PreviousBlockHash: {block.PrevHash}\n";
			output += $"MerkleRoot: {block.MerkleRoot}\n";
			output += $"NextConsensus: {block.NextConsensus}\n";
			output += $"Transactions:\n";
			foreach (Transaction t in block.Transactions)
			{
				output += $"{t.ToCLIString(block.Timestamp, disassemble)}";
				output += $"\n";
			}
			output += $"Witnesses:\n";
			output += $"\tInvocation: {block.Witness.InvocationScript.ToHexString()}\n";
			output += $"\tVerification: {block.Witness.VerificationScript.ToHexString()}\n";
			return output;
		}

		public static string ToCLIString(this Transaction t, ulong blockTimestamp = 0, bool disassemble = true)
		{	
			string output = "";
			output += $"Hash: {t.Hash}\n";
			if (blockTimestamp > 0)
			{
				output += $"Timestamp: {blockTimestamp.ToCLITimestampString()}\n";
			}
			output += $"NetFee: {new BigDecimal(t.NetworkFee, NeoToken.GAS.Decimals)}\n";
			output += $"SysFee: {new BigDecimal(t.SystemFee, NeoToken.GAS.Decimals)}\n";
			output += $"Sender: {t.Sender.ToAddress()}\n";
			if (t.Cosigners != null && t.Cosigners.Length > 0)
			{
				output += $"Cosigners:\n";
				foreach (var cosigner in t.Cosigners)
				{
					output += cosigner.ToCLIString();
				}
			}

			output += $"Script:\n";
			if(disassemble)
				output += DisassembledScriptToCLIString(t.Script);
			else
				output += t.Script.ToHexString() + "\n";

			output += "Witnesses: \n";
			foreach (var witness in t.Witnesses)
			{
				output += $"{witness.ToCLIString()}";
			}


			return output;
		}
		

		public static string ToCLIString(this ContractState c)
		{
			string output = "";
			output += $"Hash: {c.ScriptHash}\n";
			output += $"EntryPoint: \n";
			output += $"\tName: {c.Manifest.Abi.EntryPoint.Name}\n";
			output += $"\tParameters: \n";
			foreach (var parameter in c.Manifest.Abi.EntryPoint.Parameters)
			{
				output += $"\t\tName: {parameter.Name}\n";
				output += $"\t\tType: {parameter.Type}\n";
			}

			output += $"Methods: \n";
			foreach (var method in c.Manifest.Abi.Methods)
			{
				output += $"\tName: {method.Name}\n";
				output += $"\tReturn Type: {method.ReturnType}\n";
				if (method.Parameters.Length > 0)
				{
					output += $"\tParameters: \n";
					foreach (var parameter in method.Parameters)
					{
						output += $"\t\tName: {parameter.Name}\n";
						output += $"\t\tType: {parameter.Type}\n";
					}
				}
				
			}

			output += $"Events: \n";
			foreach (var abiEvent in c.Manifest.Abi.Events)
			{
				output += $"\tName: {abiEvent.Name}\n";
				if (abiEvent.Parameters.Length > 0)
				{
					output += $"\tParameters: \n";
					foreach (var parameter in abiEvent.Parameters)
					{
						output += $"\t\tName: {parameter.Name}\n";
						output += $"\t\tType: {parameter.Type}\n";
					}
				}
			}

			return output;
		}
	}
}
