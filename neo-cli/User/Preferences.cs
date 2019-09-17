using Microsoft.Extensions.Configuration;
using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.User
{
	public class Preferences
	{
		public string FaucetGitHubConfirmationUrl = "";
		public string FaucetAuthorizedAddress = "";
		public bool SkipFirstUse = false;
		public bool UseAnalytics = true;

		public static readonly Dictionary<string, string> KnownSmartContracts = new Dictionary<string, string>() {
			{ "neo", "0x43cf98eddbe047e198a3e5d57006311442a0ca15" },
			{ "gas", "0xa1760976db5fcdfab2a9930e8f6ce875b2d18225" },
			{ "policy", "0x9c5699b260bd468e2160dd5d45dfd2686bba8b77" },
		};

		private static Preferences _instance;
		public static Preferences Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = LoadDefault();
				}
				return _instance;
			}
		}

		public static string FolderPath => Path.Combine(Directory.GetCurrentDirectory(), ".preferences");
		
		public static void Save()
		{
			try
			{
				string folderPath = FolderPath;
				string filePath = Path.Combine(folderPath, "neo-preferences.json");
				var jsonRepresentation = new JObject();
				jsonRepresentation["FaucetGitHubConfirmationUrl"] = Instance.FaucetGitHubConfirmationUrl;
				jsonRepresentation["FaucetAuthorizedAddress"] = Instance.FaucetAuthorizedAddress;
				jsonRepresentation["SkipFirstUse"] = Instance.SkipFirstUse;
				jsonRepresentation["UseAnalytics"] = Instance.UseAnalytics;
				if (!File.Exists(filePath))
				{
					Directory.CreateDirectory(folderPath);
					File.Create(filePath);
				}
				File.WriteAllText(filePath, jsonRepresentation.ToString());
			}
			catch (Exception ex)
			{
				//Console.WriteLine("Preferences not saved");
			}
			
		}

		private static Preferences FromJson(JObject jsonObject)
		{
			var settings = new Preferences();
			settings.FaucetGitHubConfirmationUrl = jsonObject["FaucetGitHubConfirmationUrl"]?.AsString();
			settings.FaucetAuthorizedAddress = jsonObject["FaucetAuthorizedAddress"]?.AsString();
			settings.SkipFirstUse = (bool)jsonObject["SkipFirstUse"]?.AsBoolean();
			settings.UseAnalytics = (bool)jsonObject["UseAnalytics"]?.AsBoolean();
			return settings;
		}

		private static Preferences LoadDefault()
		{
			string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), ".neo", "neo-preferences.json");

			if (File.Exists(path))
			{
				try
				{
					string jsonContent = File.ReadAllText(path);
					var jObjectSettings = JObject.Parse(jsonContent);
					var settings = FromJson(jObjectSettings);
					return settings;
				}
				catch (Exception ex)
				{
					return new Preferences();
				}
			}
			else
			{
				return new Preferences();
			}

		}

	}
}
